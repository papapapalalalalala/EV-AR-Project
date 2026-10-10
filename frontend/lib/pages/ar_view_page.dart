import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';

import '../theme/app_theme.dart';

/// หน้าแสดงผล AR ที่ฝัง Unity (embedded) ไว้เต็มจอ
///
/// Unity scene ฝั่งนี้มี GameObject ชื่อ "FlutterBridge" (component
/// FlutterBridge.cs) ที่รับ vehicleId ผ่าน sendToUnity(...) จากหน้า
/// ScanQRPage อยู่แล้วตั้งแต่ก่อนเข้าหน้านี้ (ดู scan_qr_page.dart) -
/// ดังนั้นแค่ต้องแสดง widget EmbedUnity ให้เต็มจอเฉยๆ
///
/// หมายเหตุ: "Unity can only be shown in 1 widget at a time" (ข้อจำกัดของ
/// flutter_embed_unity) - ถ้ามีมากกว่า 1 widget ที่ embed Unity พร้อมกัน
/// (เช่น สลับหน้าไปมาเร็วๆ) Unity view จะถูก detach/reattach อัตโนมัติ
///
/// ข้อจำกัดสำคัญ: flutter_embed_unity รองรับแค่ Android/iOS เท่านั้น (ใช้
/// PlatformView ของมือถือฝัง Unity) - บน Flutter Web ตัว plugin เองจะ throw
/// "Unsupported platform" ทันทีตอน build widget EmbedUnity (ไม่ใช่บั๊กของเรา
/// แก้ที่โค้ดเราไม่ได้ เพราะ plugin ไม่มี implementation ฝั่ง web ให้เลย และ
/// Unity AR Foundation/marker tracking เองก็ไม่รองรับ WebGL build ด้วยเช่นกัน)
/// เลยเช็ค kIsWeb ไว้ก่อน แล้วโชว์การ์ดอธิบายแทนที่จะปล่อยให้แอป crash แดง
/// ทั้งจอ - ฟีเจอร์อื่น (สแกน QR / ดูข้อมูลสด) ยังใช้ได้ปกติทั้งมือถือและเว็บ
class ARViewPage extends StatelessWidget {
  final String vehicleId;

  const ARViewPage({super.key, required this.vehicleId});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('AR - $vehicleId'),
        centerTitle: true,
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
      ),
      backgroundColor: Colors.black,
      body: kIsWeb ? _buildWebFallback(context) : _buildUnityView(),
    );
  }

  Widget _buildUnityView() {
    return EmbedUnity(
      onMessageFromUnity: (String message) {
        // ข้อความที่ Unity ส่งกลับมา (ผ่าน FlutterBridge.NotifyFlutter)
        debugPrint('[ARViewPage] message from Unity: $message');
      },
    );
  }

  // ==========================================
  // Fallback ตอนรันบน Flutter Web - แทนที่จะปล่อยให้ EmbedUnity throw
  // "Unsupported platform" จน Flutter โชว์ error screen สีแดงเต็มจอ
  // ==========================================
  Widget _buildWebFallback(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 96,
              height: 96,
              decoration: BoxDecoration(
                color: AppColors.primary.withValues(alpha: 0.15),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.view_in_ar_outlined,
                size: 52,
                color: AppColors.primary,
              ),
            ),
            const SizedBox(height: 24),
            const Text(
              'มุมมอง AR ใช้ได้เฉพาะแอปมือถือ',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w800,
                color: Colors.white,
              ),
            ),
            const SizedBox(height: 10),
            const Text(
              'เพราะต้องใช้กล้องสแกน Marker บนตัวรถจริงผ่าน Android/iOS - '
              'บนเว็บเบราว์เซอร์ยังไม่รองรับ (ทั้งจากข้อจำกัดของ plugin ที่ฝัง '
              'Unity และ AR marker tracking เอง)',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 14, color: Colors.white70, height: 1.5),
            ),
            const SizedBox(height: 28),
            OutlinedButton.icon(
              onPressed: () => Navigator.pop(context),
              style: OutlinedButton.styleFrom(
                foregroundColor: Colors.white,
                side: const BorderSide(color: Colors.white54),
              ),
              icon: const Icon(Icons.arrow_back),
              label: const Text('กลับไปดูข้อมูลสด'),
            ),
          ],
        ),
      ),
    );
  }
}
