import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('validate then submit a new vehicle request for admin approval', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    final signInFields = find.byType(TextField);
    await tester.enterText(signInFields.at(0), 'salestest');
    await tester.enterText(signInFields.at(1), 'SalesTest123!');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    await tester.tap(find.text('Request Vehicle'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('REQUEST NEW VEHICLE'), findsOneWidget);

    // validation: nothing filled in
    await tester.tap(find.widgetWithText(ElevatedButton, 'Submit for approval'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('Enter a driver name'), findsOneWidget);

    final regNo = 'REQ-${DateTime.now().millisecondsSinceEpoch}';
    final fields = find.byType(TextField);
    await tester.tap(fields.at(0));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(fields.at(0), 'Yaw Boateng');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.tap(fields.at(1));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(fields.at(1), regNo);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.tap(fields.at(2));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(fields.at(2), '0244000111');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.tap(fields.at(3));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(fields.at(3), '3');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    FocusManager.instance.primaryFocus?.unfocus();
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Submit for approval'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Submit for approval'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('was submitted for admin approval'), findsOneWidget);
    expect(find.textContaining(regNo), findsOneWidget);
  });
}
