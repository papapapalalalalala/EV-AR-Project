import 'package:flutter/material.dart';

import 'pages/home_page.dart';
import 'theme/app_theme.dart';

void main() {
  runApp(const EVApp());
}

class EVApp extends StatelessWidget {
  const EVApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'EV Monitor',
      theme: AppTheme.light,
      home: const HomePage(),
    );
  }
}
