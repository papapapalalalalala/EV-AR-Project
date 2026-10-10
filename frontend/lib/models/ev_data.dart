class EVData {
  final int id;
  final String vehicleId;
  final double soc;
  final double voltage;
  final double batteryTemperature;
  final int motorRpm;
  final double motorPower;
  final String status;
  final String createdAt;

  EVData({
    required this.id,
    required this.vehicleId,
    required this.soc,
    required this.voltage,
    required this.batteryTemperature,
    required this.motorRpm,
    required this.motorPower,
    required this.status,
    required this.createdAt,
  });

  factory EVData.fromJson(Map<String, dynamic> json) {
    return EVData(
      id: int.parse(json['id'].toString()),
      vehicleId: json['vehicle_id'].toString(),
      soc: double.parse(json['soc'].toString()),
      voltage: double.parse(json['voltage'].toString()),
      batteryTemperature: double.parse(json['battery_temperature'].toString()),
      motorRpm: int.parse(json['motor_rpm'].toString()),
      motorPower: double.parse(json['motor_power'].toString()),
      status: json['status'].toString(),
      createdAt: json['created_at'].toString(),
    );
  }

  // ==========================================
  // แปลงเป็น JSON เพื่อส่งเข้า Unity ผ่าน
  // sendToUnity('FlutterBridge', 'SetEVData', jsonEncode(data.toJson()))
  // ชื่อ field ต้องตรงกับ FlutterBridge.EVDataPayload ฝั่ง Unity
  // (Assets/Scripts/FlutterBridge.cs) เป๊ะๆ เพราะ JsonUtility map แบบ 1:1
  // ==========================================
  Map<String, dynamic> toJson() {
    return {
      'vehicleId': vehicleId,
      'soc': soc,
      'voltage': voltage,
      'batteryTemperature': batteryTemperature,
      'motorRpm': motorRpm,
      'motorPower': motorPower,
      'status': status,
    };
  }
}
