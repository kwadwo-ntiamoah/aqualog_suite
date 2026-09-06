import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:aqualog_mobile/main.dart';

// Throwaway account provisioned via `POST /auth/user` before this test was
// written (see project_sales_app_flutter.md M6 notes) — a real password
// change can't safely run against a shared account like salestest, since it
// would break every other test that hardcodes that password. The account's
// real password toggles between these two values on every successful run
// (a completed run leaves it on _newPassword), so the two are swapped here
// after each green run to keep the test idempotent across reruns instead of
// going stale after one use.
const _username = 'pwtest1788646432';
const _oldPassword = 'PwTestNew456!';
const _newPassword = 'PwTest123!';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('shows the signed-in user, rejects a wrong current password, then changes it', (tester) async {
    SharedPreferences.setMockInitialValues({});
    await tester.pumpWidget(const AquaLogSalesApp());
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    final signInFields = find.byType(TextField);
    await tester.enterText(signInFields.at(0), _username);
    await tester.enterText(signInFields.at(1), _oldPassword);
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    await tester.tap(find.text('Account'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text(_username), findsOneWidget);

    // TextField order on this screen: [0] disabled username, [1] current
    // password, [2] new password.
    final passwordFields = find.byType(TextField);
    await tester.tap(passwordFields.at(1));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(passwordFields.at(1), 'WrongCurrentPass1!');
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.tap(passwordFields.at(2));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(passwordFields.at(2), _newPassword);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    FocusManager.instance.primaryFocus?.unfocus();
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Update password'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Update password'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.textContaining('error occurred changing password'), findsOneWidget);

    await tester.tap(passwordFields.at(1));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    await tester.enterText(passwordFields.at(1), _oldPassword);
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));
    FocusManager.instance.primaryFocus?.unfocus();
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 10));

    await tester.ensureVisible(find.widgetWithText(ElevatedButton, 'Update password'));
    await tester.tap(find.widgetWithText(ElevatedButton, 'Update password'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('Password updated.'), findsOneWidget);

    // sign out and confirm the new password actually works
    await tester.tap(find.text('Sign out'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));

    final resignInFields = find.byType(TextField);
    await tester.enterText(resignInFields.at(0), _username);
    await tester.enterText(resignInFields.at(1), _newPassword);
    await tester.tap(find.widgetWithText(ElevatedButton, 'Sign in'));
    await tester.pumpAndSettle(const Duration(milliseconds: 200), EnginePhase.sendSemanticsUpdate, const Duration(seconds: 20));
    expect(find.text('Dashboard'), findsOneWidget);
  });
}
