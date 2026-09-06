import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('forgot password: unknown user shows an error, known user notifies the admin', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    await tester.tap(find.text('Forgot password?'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('Reset your password'), findsOneWidget);

    final usernameField = find.byType(TextField).first;

    await tester.enterText(usernameField, '__no_such_user__');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Notify admin'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('No account found'), findsOneWidget);

    await tester.tap(usernameField);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(usernameField, 'salestest');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    FocusManager.instance.primaryFocus?.unfocus();
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.tap(find.widgetWithText(ElevatedButton, 'Notify admin'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('Your admin has been notified.'), findsOneWidget);
  });
}
