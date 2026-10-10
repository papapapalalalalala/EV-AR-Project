#!/usr/bin/env ruby
# Wires the exported Unity iOS Xcode project (ios/UnityExport) into the
# Flutter Runner project so that Runner.xcworkspace can build an app that
# embeds Unity as a PlatformView (flutter_embed_unity).
#
# What this does, equivalent to the manual steps in flutter_embed_unity's
# iOS setup docs:
#   1. Add ios/UnityExport/Unity-iPhone.xcodeproj as a subproject reference
#      inside Runner.xcodeproj (so Xcode knows about the Unity project).
#   2. Add UnityFramework.framework (built by that subproject) to Runner's
#      "Frameworks, Libraries and Embedded Content" with "Embed & Sign".
#   3. Make Runner's build depend on the UnityFramework target so Xcode
#      builds Unity first.
#   4. Reorder build phases so "Thin Binary" (if present) runs after
#      "Embed Frameworks", avoiding a known build-cycle error.
#
# Requires the `xcodeproj` gem: gem install xcodeproj

require 'xcodeproj'

ios_dir = File.expand_path(File.join(__dir__))
runner_proj_path = File.join(ios_dir, 'Runner.xcodeproj')
unity_proj_path = File.join(ios_dir, 'UnityExport', 'Unity-iPhone.xcodeproj')

raise "Runner.xcodeproj not found at #{runner_proj_path}" unless File.exist?(runner_proj_path)
raise "Unity-iPhone.xcodeproj not found at #{unity_proj_path}" unless File.exist?(unity_proj_path)

project = Xcodeproj::Project.open(runner_proj_path)
runner_target = project.targets.find { |t| t.name == 'Runner' }
raise "Could not find Runner target" unless runner_target

# --- 1. Add Unity-iPhone.xcodeproj as a subproject reference -----------
main_group = project.main_group
existing_ref = main_group.children.find do |c|
  c.respond_to?(:path) && c.path && File.basename(c.path.to_s) == 'Unity-iPhone.xcodeproj'
end

unity_project_ref = existing_ref
unless unity_project_ref
  # Use an ABSOLUTE path here: xcodeproj's subproject handling
  # (new_subproject, triggered by the .xcodeproj extension) resolves a
  # relative path against the process's working directory rather than
  # against the group's own directory, which breaks when this script is
  # invoked from the repo root instead of from ios/.
  unity_project_ref = main_group.new_reference(unity_proj_path)
  unity_project_ref.name = 'Unity-iPhone.xcodeproj'
  puts "Added Unity-iPhone.xcodeproj reference at #{unity_proj_path}"
else
  puts "Unity-iPhone.xcodeproj reference already present"
end

# Load the Unity project itself to find the UnityFramework target/product
unity_project = Xcodeproj::Project.open(unity_proj_path)
unity_framework_target = unity_project.targets.find { |t| t.name == 'UnityFramework' }
raise "Could not find UnityFramework target in Unity-iPhone.xcodeproj" unless unity_framework_target

unity_framework_product = unity_framework_target.product_reference
puts "Found UnityFramework product: #{unity_framework_product.path}"

# --- 1b. Replicate flutter_embed_unity's required iOS export transforms -
# Normally applied automatically by the FlutterEmbed Unity package's own
# custom export menu command (ProjectExporterIos.TransformExportedProject
# in the Unity package's Editor scripts). This export was produced with
# Unity's plain Build Profiles > Build instead, which skips that step, so
# we replicate the same 3 transforms here in Ruby, directly against
# Unity-iPhone.xcodeproj.

# 1. Disable bitcode (the -U linker flag below is incompatible with it;
#    bitcode is deprecated by Apple anyway).
unity_project.build_configurations.each do |config|
  config.build_settings['ENABLE_BITCODE'] = 'NO'
end

# 2. FlutterEmbedUnityIos_sendToFlutter is declared (but not defined) in
#    Unity's compiled code; it's actually defined in the Flutter plugin's
#    native code. Tell the linker to leave it unresolved for now instead
#    of failing, so it can be resolved later when UnityFramework is linked
#    into Runner (this is exactly the error we hit last run).
unity_ldflag = '-Wl,-U,_FlutterEmbedUnityIos_sendToFlutter'
unity_project.build_configurations.each do |config|
  existing = config.build_settings['OTHER_LDFLAGS']
  case existing
  when nil
    config.build_settings['OTHER_LDFLAGS'] = ['$(inherited)', unity_ldflag]
  when String
    config.build_settings['OTHER_LDFLAGS'] = [existing, unity_ldflag].uniq
  when Array
    config.build_settings['OTHER_LDFLAGS'] = (existing + [unity_ldflag]).uniq
  end
end
puts "Disabled bitcode and added -U linker flag for FlutterEmbedUnityIos_sendToFlutter"

# 3. Move the "Data" folder's target membership from Unity-iPhone (app) to
#    UnityFramework, so it travels with the embedded framework instead of
#    the app binary.
data_ref = unity_project.files.find { |f| f.path.to_s == 'Data' }
if data_ref
  unity_main_target = unity_project.targets.find { |t| t.name == 'Unity-iPhone' }
  unity_main_target.resources_build_phase.remove_file_reference(data_ref) if unity_main_target

  already_in_framework = unity_framework_target.resources_build_phase.files.any? { |bf| bf.file_ref == data_ref }
  unity_framework_target.resources_build_phase.add_file_reference(data_ref) unless already_in_framework
  puts "Moved Data folder target membership: Unity-iPhone -> UnityFramework"
else
  puts "WARNING: could not find 'Data' folder reference in Unity-iPhone.xcodeproj to move"
end

unity_project.save
puts "Saved Unity-iPhone.xcodeproj with bitcode/linker/Data fixes"

# --- 2. Embed UnityFramework.framework into Runner ----------------------
frameworks_phase = runner_target.frameworks_build_phase

has_unity_framework = frameworks_phase.files.any? do |bf|
  bf.file_ref && bf.file_ref.path.to_s.include?('UnityFramework')
end

if has_unity_framework
  puts "UnityFramework already linked in Frameworks build phase"
else
  # Create (or reuse) a PBXFileReference under a "Frameworks" group pointing
  # to the product built by the Unity subproject, then add it both to the
  # Frameworks build phase (link) and an Embed Frameworks phase (embed+sign).
  proxy_file_ref = project.new(Xcodeproj::Project::Object::PBXFileReference)
  proxy_file_ref.path = 'UnityFramework.framework'
  proxy_file_ref.source_tree = 'BUILT_PRODUCTS_DIR'
  proxy_file_ref.include_in_index = '0'

  frameworks_phase.add_file_reference(proxy_file_ref)
  puts "Linked UnityFramework.framework in Frameworks build phase"

  embed_phase = runner_target.build_phases.find { |p| p.respond_to?(:name) && p.name == 'Embed Frameworks' }
  embed_phase ||= runner_target.new_copy_files_build_phase('Embed Frameworks')
  embed_phase.symbol_dst_subfolder_spec = :frameworks
  embed_build_file = embed_phase.add_file_reference(proxy_file_ref)
  embed_build_file.settings ||= {}
  embed_build_file.settings['ATTRIBUTES'] = ['CodeSignOnCopy', 'RemoveHeadersOnCopy']
  puts "Added UnityFramework.framework to Embed Frameworks (Embed & Sign)"
end

# --- 3. Make Runner depend on the UnityFramework target -----------------
has_dependency = runner_target.dependencies.any? do |dep|
  dep.target && dep.target.name == 'UnityFramework'
end

unless has_dependency
  container_proxy = project.new(Xcodeproj::Project::Object::PBXContainerItemProxy)
  container_proxy.container_portal = unity_project_ref.uuid
  container_proxy.proxy_type = '1'
  container_proxy.remote_global_id_string = unity_framework_target.uuid
  container_proxy.remote_info = 'UnityFramework'

  target_dependency = project.new(Xcodeproj::Project::Object::PBXTargetDependency)
  target_dependency.name = 'UnityFramework'
  target_dependency.target_proxy = container_proxy

  runner_target.dependencies << target_dependency
  puts "Added Runner -> UnityFramework target dependency"
else
  puts "Runner already depends on UnityFramework"
end

# --- 4. Reorder Thin Binary (if present) after Embed Frameworks ---------
thin_binary_phase = runner_target.build_phases.find { |p| p.respond_to?(:name) && p.name == 'Thin Binary' }
embed_phase = runner_target.build_phases.find { |p| p.respond_to?(:name) && p.name == 'Embed Frameworks' }

if thin_binary_phase && embed_phase
  phases = runner_target.build_phases
  phases.delete(thin_binary_phase)
  embed_index = phases.index(embed_phase)
  phases.insert(embed_index + 1, thin_binary_phase)
  puts "Reordered Thin Binary to run after Embed Frameworks"
end

project.save
puts "Saved Runner.xcodeproj with Unity wiring."
