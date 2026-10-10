import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:flutter_embed_unity/flutter_embed_unity.dart';

import '../models/ev_data.dart';
import '../services/api_service.dart';
import '../theme/app_theme.dart';
import 'ar_view_page.dart';

class EVDataPage extends StatefulWidget {
  final String vehicleId;

  const EVDataPage({super.key, required this.vehicleId});

  @override
  State<EVDataPage> createState() => _EVDataPageState();
}

class _EVDataPageState extends State<EVDataPage> {
  EVData? data;
  String? errorMessage;

  Timer? timer;

  bool isLoading = true;

  @override
  void initState() {
    super.initState();

    // โหลดข้อมูลครั้งแรก
    loadData();

    // โหลดข้อมูลใหม่ทุก 5 วินาที
    timer = Timer.periodic(const Duration(seconds: 5), (timer) {
      loadData();
    });
  }

  // ==========================================
  // ดึงข้อมูลจาก API
  // ==========================================

  Future<void> loadData() async {
    try {
      final result = await ApiService.getEVData(widget.vehicleId);

      if (!mounted) return;

      setState(() {
        data = result;
        errorMessage = null;
        isLoading = false;
      });

      // ส่งข้อมูลล่าสุดเข้า Unity ด้วย ทุกครั้งที่ poll สำเร็จ (แม้ตอนนี้จะยัง
      // ไม่ได้เปิดหน้า AR อยู่ก็ตาม) เพื่อให้การ์ด AR overlay อัปเดตสดเมื่อ
      // ผู้ใช้กดเข้าไปดูทีหลัง - ดู EVDataCardUI.cs ฝั่ง Unity
      _sendDataToUnity(result);
    } catch (error) {
      if (!mounted) return;

      setState(() {
        errorMessage = error.toString();
        isLoading = false;
      });
    }
  }

  void _sendDataToUnity(EVData result) {
    // flutter_embed_unity ไม่รองรับ Flutter Web (ดูคอมเมนต์ใน ar_view_page.dart)
    // ข้ามไปเลยบนเว็บ กันไม่ให้ debug console เต็มไปด้วย error ทุก 5 วิ
    if (kIsWeb) return;

    try {
      sendToUnity('FlutterBridge', 'SetEVData', jsonEncode(result.toJson()));
    } catch (e) {
      // ไม่กระทบการแสดงผลในแอป
      // ignore: avoid_print
      print('_sendDataToUnity failed: $e');
    }
  }

  // ==========================================
  // หยุด Timer เมื่อออกจากหน้า
  // ==========================================

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  // ==========================================
  // UI
  // ==========================================

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.vehicleId)),

      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : errorMessage != null && data == null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(20),

                child: Text(
                  'ไม่สามารถโหลดข้อมูลได้\n\n'
                  '$errorMessage',
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: AppColors.textSecondary),
                ),
              ),
            )
          : data == null
          ? const Center(child: Text('ไม่พบข้อมูลรถ'))
          : buildEVData(data!),

      // ======================================
      // ปุ่มไปหน้า AR (embedded Unity view)
      // ======================================
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => ARViewPage(vehicleId: widget.vehicleId),
            ),
          );
        },
        icon: const Icon(Icons.view_in_ar),
        label: const Text('ดู AR'),
      ),
    );
  }

  // ==========================================
  // แสดงข้อมูล EV
  // ==========================================

  Widget buildEVData(EVData data) {
    final bool normal = data.status == 'NORMAL';

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 100),

      children: [
        // ======================================
        // หัวการ์ด: ไอคอนรถ + รหัสรถ + สถานะ
        // ======================================
        Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(22),
            border: Border.all(color: AppColors.border),
          ),
          child: Column(
            children: [
              Container(
                width: 84,
                height: 84,
                decoration: const BoxDecoration(
                  color: AppColors.primaryLight,
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.electric_car,
                  size: 46,
                  color: AppColors.primary,
                ),
              ),

              const SizedBox(height: 14),

              Text(
                data.vehicleId,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),

              const SizedBox(height: 12),

              // สถานะ - เป็น pill
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: normal ? AppColors.primaryLight : AppColors.dangerBg,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  data.status,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: normal ? AppColors.primaryDark : AppColors.danger,
                  ),
                ),
              ),

              const SizedBox(height: 14),

              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    width: 8,
                    height: 8,
                    decoration: const BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 8),
                  const Text(
                    'Live Data • อัปเดตทุก 5 วินาที',
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),

        const SizedBox(height: 20),

        // ======================================
        // SOC - แถบ progress
        // ======================================
        _socCard(data.soc),

        const SizedBox(height: 12),

        // ======================================
        // ตารางสถิติ 2 คอลัมน์
        // ======================================
        Row(
          children: [
            Expanded(
              child: _statCard(
                icon: Icons.bolt,
                title: 'Voltage',
                value: '${data.voltage.toStringAsFixed(0)} V',
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _statCard(
                icon: Icons.thermostat,
                title: 'Battery Temp',
                value: '${data.batteryTemperature.toStringAsFixed(0)} °C',
              ),
            ),
          ],
        ),

        const SizedBox(height: 12),

        Row(
          children: [
            Expanded(
              child: _statCard(
                icon: Icons.settings,
                title: 'Motor RPM',
                value: '${data.motorRpm} RPM',
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _statCard(
                icon: Icons.electric_bolt,
                title: 'Motor Power',
                value: '${data.motorPower.toStringAsFixed(0)} kW',
              ),
            ),
          ],
        ),
      ],
    );
  }

  // ==========================================
  // การ์ด SOC พร้อมแถบ progress สีเขียว
  // ==========================================
  Widget _socCard(double soc) {
    final clamped = (soc / 100).clamp(0.0, 1.0);

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(
                Icons.battery_full,
                size: 20,
                color: AppColors.primary,
              ),
              const SizedBox(width: 8),
              const Text(
                'SOC',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                ),
              ),
              const Spacer(),
              Text(
                '${soc.toStringAsFixed(0)}%',
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(999),
            child: LinearProgressIndicator(
              value: clamped,
              minHeight: 10,
              backgroundColor: AppColors.primaryLight,
              valueColor: const AlwaysStoppedAnimation(AppColors.primary),
            ),
          ),
        ],
      ),
    );
  }

  // ==========================================
  // การ์ดสถิติทั่วไป (ไอคอน + ชื่อ + ค่า)
  // ==========================================
  Widget _statCard({
    required IconData icon,
    required String title,
    required String value,
  }) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: const BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 18, color: AppColors.primary),
          ),
          const SizedBox(height: 10),
          Text(
            title,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            value,
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
        ],
      ),
    );
  }
}
