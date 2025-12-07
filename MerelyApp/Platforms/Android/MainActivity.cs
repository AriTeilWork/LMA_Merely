using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace MerelyApp;

[Activity(Label = "MerelyApp", Icon = "@mipmap/appicon", Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    const int RequestLocationId = 1000;

    readonly string[] LocationPermissions = new string[]
    {
        Android.Manifest.Permission.AccessFineLocation,
        Android.Manifest.Permission.AccessCoarseLocation
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Request location permissions proactively on newer Android versions
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.AccessFineLocation) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, LocationPermissions, RequestLocationId);
            }
        }
    }

    #pragma warning disable CA1416 // Validate platform compatibility
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [Android.Runtime.GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    #pragma warning restore CA1416 // Validate platform compatibility
}
