using System;
using Sentry;
using Sentry.Protocol;

namespace AINotes.Helpers; 

public static class SentryHelper {
    public static void CaptureCaughtException(Exception ex, bool handled=true, string mechanismKey="CaptureCaughtException") {
        ex.Data[Mechanism.HandledKey] = handled;
        ex.Data[Mechanism.MechanismKey] = mechanismKey;
        SentrySdk.CaptureException(ex);
    }
}