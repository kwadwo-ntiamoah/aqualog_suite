import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

void main() {
  testWidgets('shows the sign-in screen when no session is stored', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle();

    expect(find.text('AquaLog'), findsOneWidget);
    expect(find.text('SALES APP'), findsOneWidget);
    expect(find.widgetWithText(ElevatedButton, 'Sign in'), findsOneWidget);
  });
}
