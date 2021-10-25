using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AddNewPlan;


// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.3")]
[assembly: AssemblyFileVersion("1.0.0.3")]
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
            window.Title = "AutoAddBeam";
            window.Height = 190;
            window.Width = 290;
            Form form1 = new Form();
            form1.StartPosition = FormStartPosition.CenterScreen;
        }
    }
}
