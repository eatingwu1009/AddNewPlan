using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AddNewBeam
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public List<String> MachineName { get; set; }
        public List<double> IsoList { get; set; }
        public ScriptContext SC { get; set; }
        public VMS.TPS.Common.Model.API.Image SI { get; set; }
        public StructureSet SS { get; set; }
        public VVector SIU { get; set; }
        //public VVector SIO { get; set; }
        public UserControl1(ScriptContext scriptContext)
        {
            SC = scriptContext;
            SS = SC.StructureSet;
            SI = SC.Image;
            SIU = SI.UserOrigin;
            //SIO = SI.Origin;
            IsoList = new List<double>();

            MachineName = new List<String>();
            MachineName.Add("LA3TB1623");
            MachineName.Add("LA5TB2069");
            MachineName.Add("LA6TB4313");
            MachineName.Add("LA7TB4557");
            //MachineName.Add("iX 1100");

            InitializeComponent();
            DataContext = this;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SC.Patient.BeginModifications();
            Course course = SC.Patient.Courses.Where(s => s.Id =="AutoLoad").FirstOrDefault();
            if (course is null)
            {
                course = SC.Patient.AddCourse();
                course.Id = "AutoLoad";
            }

            StructureSet ss = SC.Patient.StructureSets.Where(s => s.Id == SI.Id).FirstOrDefault();
            if (ss is null)
            {
                ss = SI.CreateNewStructureSet();
                ss.Id = SI.Id;
            }

            string PLANID = string.Empty;
            if (ss.Id.Length < 9) { PLANID = "Load" + ss.Id; } else { PLANID = ss.Id; }
            ExternalPlanSetup plan = course.ExternalPlanSetups.Where(s => s.Id == PLANID).FirstOrDefault();
            if (plan is null)
            {
                plan = course.AddExternalPlanSetup(ss);
                plan.Id = PLANID;
            }

            var BodyPar = ss.GetDefaultSearchBodyParameters();
            ss.CreateAndSearchBody(BodyPar);

            string LINACID = machine.SelectedItem.ToString();
            ExternalBeamMachineParameters beamparams = new ExternalBeamMachineParameters(LINACID, "6X", 600, "STATIC", null);
            plan.AddMLCBeam(beamparams, null, new VRect<double>(-50, -50, 50, 50), 0, 0, 0, SIU);


            if ((bool)MultipleIsocenter.IsChecked)
            {
                var MulIso = SS.Structures.Where(s => s.DicomType == "MARKER").ToList();
                foreach (Structure Iso in MulIso)
                    {
                        VVector Isolocation = Iso.CenterPoint;
                        plan.AddMLCBeam(beamparams, null, new VRect<double>(-50, -50, 50, 50), 0, 0, 0, Isolocation);
                    }
             }


            var myDRR = new DRRCalculationParameters(500); // 500mm is the DRR size
            myDRR.SetLayerParameters(0, 0.1, -550, 0, -100, 100); // Layer 1
            myDRR.SetLayerParameters(1, 0.9, 100, 1000, -100, 100); // Layer 2

            foreach (Beam beam in plan.Beams)
            { beam.CreateOrReplaceDRR(myDRR);}

            Window.GetWindow(this).Close();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            string MarkerDescript = string.Empty;

            int a = 1;
            var MulIso = SS.Structures.Where(s => s.DicomType == "MARKER").ToList();
            MarkerDescript += "\nUser Origin\tx:0\ty:0\tz:0";
            foreach (Structure Iso in MulIso)
            {
                MarkerDescript += "\nIsocenter" + a + "\t\tx:" + Math.Round((Iso.CenterPoint.x - SIU.x) / 10,2) + "\ty:" + Math.Round((Iso.CenterPoint.y - SIU.y) / 10,2) + "\tz:" + Math.Round((Iso.CenterPoint.z - SIU.z) / 10,2);
                a = a + 1;
            }
            //MessageBox.Show(Convert.ToInt32(SIU.x).ToString()+","+ Convert.ToInt32(SIU.y).ToString()+ "," + Convert.ToInt32(SIU.z).ToString() + "," + Convert.ToInt32(SIO.x).ToString()+ "," + Convert.ToInt32(SIO.y).ToString()+ "," + Convert.ToInt32(SIO.z).ToString());
            MessageBox.Show("You will Apply the following Beams\n------------------------------------------------------" + MarkerDescript);
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            string MarkerDescript = string.Empty;
            MessageBox.Show("You will NOT apply multiple isocenters");
        }
    }

}
