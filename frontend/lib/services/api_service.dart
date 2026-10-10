import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../models/ev_data.dart';

class ApiService {
  static String get baseUrl {
    // Chrome / Flutter Web
    if (kIsWeb) {
      return 'http://localhost:3000';
    }

    // Physical Android/iOS device on the same Wi-Fi as the backend PC.
    // (10.0.2.2 only works inside the Android Emulator - not on a real phone.)
    return 'http://192.168.110.239:3000';
  }

  static Future<EVData> getEVData(String vehicleId) async {
    final url = '$baseUrl/api/ev-data/$vehicleId';

    debugPrint('Calling API: $url');

    final response = await http.get(Uri.parse(url));

    debugPrint('Status Code: ${response.statusCode}');
    debugPrint('Response: ${response.body}');

    if (response.statusCode == 200) {
      final body = jsonDecode(response.body);

      return EVData.fromJson(body['data']);
    }

    throw Exception(
      'ไม่สามารถดึงข้อมูล EV ได้\n'
      'Status: ${response.statusCode}',
    );
  }

  // ==========================================
  // แจ้ง backend ว่าเพิ่งสแกนรถคันนี้
  // Unity (ฝั่ง AR) จะ poll endpoint นี้เพื่อรู้ว่าต้องแสดงรถคันไหน
  // ยิงแบบ fire-and-forget: ถ้าพลาดก็แค่ log ไว้ ไม่ block การเปิดหน้า
  // แสดงข้อมูลใน Flutter เอง
  // ==========================================
  static Future<void> notifyScan(String vehicleId) async {
    final url = '$baseUrl/api/scan';

    try {
      final response = await http.post(
        Uri.parse(url),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'vehicle_id': vehicleId}),
      );

      debugPrint('notifyScan status: ${response.statusCode}');
    } catch (e) {
      debugPrint('notifyScan failed (ไม่กระทบการแสดงผลในแอป): $e');
    }
  }
}
