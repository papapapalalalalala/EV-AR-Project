import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';

import '../services/api_service.dart';
import '../theme/app_theme.dart';
import 'ev_data_page.dart';

class ScanQRPage extends StatefulWidget {
  const ScanQRPage({super.key});

  @override
  State<ScanQRPage> createState() => _ScanQRPageState();
}

class _ScanQRPageState extends State<ScanQRPage> {
  bool scanned = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        title: const Text('Scan QR Code'),
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        elevation: 0,
      ),

      body: Stack(
        fit: StackFit.expand,
        children: [
          // ======================================
          // กล้องสแกน QR (เต็มจอ)
          // ======================================
          MobileScanner(
            onDetect: (capture) {
              if (scanned) return;

              final List<Barcode> barcodes = capture.barcodes;

              if (barcodes.isEmpty) return;

              final String? vehicleId = barcodes.first.rawValue;

              if (vehicleId == null) return;

              scanned = true;

              // แจ้ง backend ว่าเพิ่งสแกนรถคันนี้ - ใช้เป็น fallback/ที่มาข้อมูลหลัก
              // สำหรับหน้า EVDataPage (polling REST API ทุก 5 วิ)
              ApiService.notifyScan(vehicleId);

              // ส่ง vehicleId เข้าไปยัง Unity ที่ฝังอยู่ในแอปโดยตรง ผ่าน
              // flutter_embed_unity -> UnitySendMessage("FlutterBridge", "SetVehicleId", vehicleId)
              // ฝั่ง Unity ต้องมี GameObject ชื่อ "FlutterBridge" (ตรงตัว) พร้อม
              // component FlutterBridge.cs อยู่ใน Scene แรกที่โหลด (ดู Assets/Scripts/FlutterBridge.cs)
              _sendVehicleIdToUnity(vehicleId);

              Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (context) => EVDataPage(vehicleId: vehicleId),
                ),
              );
            },
          ),

          // ======================================
          // กรอบสแกนสีเขียว มุมโค้ง แบบ minimal
          // ======================================
          Center(
            child: Container(
              width: 260,
              height: 260,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(28),
                border: Border.all(color: AppColors.primary, width: 3),
              ),
              child: Stack(
                children: const [
                  _ScanCorner(alignment: Alignment.topLeft),
                  _ScanCorner(alignment: Alignment.topRight),
                  _ScanCorner(alignment: Alignment.bottomLeft),
                  _ScanCorner(alignment: Alignment.bottomRight),
                ],
              ),
            ),
          ),

          // ======================================
          // การ์ดคำแนะนำด้านล่าง
          // ======================================
          Positioned(
            left: 20,
            right: 20,
            bottom: 32,
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 20,
                vertical: 18,
              ),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(18),
              ),
              child: Row(
                children: [
                  const Icon(Icons.qr_code_2, color: AppColors.primary),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Text(
                          'สแกน QR Code ของรถ EV',
                          style: TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 15,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        SizedBox(height: 2),
                        Text(
                          'นำกล้องไปส่อง QR Code ที่ติดอยู่บนตัวรถ',
                          style: TextStyle(
                            fontSize: 13,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _sendVehicleIdToUnity(String vehicleId) {
    // flutter_embed_unity ไม่รองรับ Flutter Web (ดูคอมเมนต์ใน ar_view_page.dart)
    if (kIsWeb) return;

    try {
      sendToUnity('FlutterBridge', 'SetVehicleId', vehicleId);
    } catch (e) {
      // ไม่กระทบการแสดงผลในแอป (เช่น Unity view ยังไม่ทันถูกสร้าง)
      // ignore: avoid_print
      print('_sendVehicleIdToUnity failed: $e');
    }
  }
}

/// มุมกรอบสแกน 4 มุม (L-shape) แบบ minimal - แทนกรอบเหลี่ยมทึบทั้งเส้น
class _ScanCorner extends StatelessWidget {
  final Alignment alignment;

  const _ScanCorner({required this.alignment});

  @override
  Widget build(BuildContext context) {
    final isTop = alignment.y < 0;
    final isLeft = alignment.x < 0;

    return Align(
      alignment: alignment,
      child: Container(
        width: 30,
        height: 30,
        decoration: BoxDecoration(
          border: Border(
            top: isTop
                ? const BorderSide(color: Colors.white, width: 4)
                : BorderSide.none,
            bottom: !isTop
                ? const BorderSide(color: Colors.white, width: 4)
                : BorderSide.none,
            left: isLeft
                ? const BorderSide(color: Colors.white, width: 4)
                : BorderSide.none,
            right: !isLeft
                ? const BorderSide(color: Colors.white, width: 4)
                : BorderSide.none,
          ),
        ),
      ),
    );
  }
}
