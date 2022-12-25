using MelonLoader;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle(Showstopper.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(Showstopper.BuildInfo.Company)]
[assembly: AssemblyProduct(Showstopper.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + Showstopper.BuildInfo.Author)]
[assembly: AssemblyTrademark(Showstopper.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
//[assembly: Guid("")]
[assembly: AssemblyVersion(Showstopper.BuildInfo.Version)]
[assembly: AssemblyFileVersion(Showstopper.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: MelonInfo(typeof(Showstopper.Showstopper), Showstopper.BuildInfo.Name, Showstopper.BuildInfo.Version, Showstopper.BuildInfo.Author, Showstopper.BuildInfo.DownloadLink)]


// Create and Setup a MelonModGame to mark a Mod as Universal or Compatible with specific Games.
// If no MelonModGameAttribute is found or any of the Values for any MelonModGame on the Mod is null or empty it will be assumed the Mod is Universal.
// Values for MelonModGame can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.
[assembly: MelonGame("Stress Level Zero", "BONELAB")]