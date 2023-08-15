using System.Reflection;
using VMS.TPS.Common.Model.API;
using System.Windows.Forms;
using AddNewBeam;


// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.11")]
[assembly: AssemblyFileVersion("1.0.0.11")]
[assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
[assembly: ESAPIScript(IsWriteable = true)]


namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {

        }
        public void Execute(ScriptContext scriptcontext, System.Windows.Window window, ScriptEnvironment environment)
        {
            UserControl1 userControl = new UserControl1(scriptcontext);
            window.Content = userControl;
            window.Title = "AutoAddPlan_EatingWu🌼";
            window.Height = 220;
            window.Width = 435;
            Form form1 = new Form();
            form1.StartPosition = FormStartPosition.CenterScreen;
        }
    }
}
