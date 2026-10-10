#include "pch-cpp.hpp"






struct SendToFlutter_tFB22A5CB193AC448C2E8FA7038B51B3F3EF7CBFF;
struct String_t;



IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_t74B04E9977998F4CF0D189D867CF72112A318590 
{
};
struct SendToFlutter_tFB22A5CB193AC448C2E8FA7038B51B3F3EF7CBFF  : public RuntimeObject
{
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif



IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SendToFlutter_FlutterEmbedUnityIos_sendToFlutter_m91722737CF058B8C804AC1CFB463ED7D2DEA72CB (String_t* ___0_data, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C void DEFAULT_CALL FlutterEmbedUnityIos_sendToFlutter(char*);
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SendToFlutter_Send_m46A64C268F0E9BEB106502B03668E140B756C393 (String_t* ___0_data, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_data;
		SendToFlutter_FlutterEmbedUnityIos_sendToFlutter_m91722737CF058B8C804AC1CFB463ED7D2DEA72CB(L_0, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SendToFlutter_FlutterEmbedUnityIos_sendToFlutter_m91722737CF058B8C804AC1CFB463ED7D2DEA72CB (String_t* ___0_data, const RuntimeMethod* method) 
{
	typedef void (DEFAULT_CALL *PInvokeFunc) (char*);

	char* ____0_data_marshaled = NULL;
	____0_data_marshaled = il2cpp_codegen_marshal_string(___0_data);

	reinterpret_cast<PInvokeFunc>(FlutterEmbedUnityIos_sendToFlutter)(____0_data_marshaled);

	il2cpp_codegen_marshal_free(____0_data_marshaled);
	____0_data_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void SendToFlutter__ctor_m2228BB79569188ED752746B0175D86941F71D37B (SendToFlutter_tFB22A5CB193AC448C2E8FA7038B51B3F3EF7CBFF* __this, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
