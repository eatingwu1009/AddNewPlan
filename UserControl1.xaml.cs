using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Forms;

namespace AddNewBeam
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : System.Windows.Controls.UserControl
    {
        public List<String> MachineName { get; set; }
        public List<double> IsoList { get; set; }
        public ScriptContext SC { get; set; }
        public VMS.TPS.Common.Model.API.Image SI { get; set; }
        public StructureSet SS { get; set; }
        public VVector SIU { get; set; }
        public bool XamlAddCouch { get; set; }
        //public VVector SIO { get; set; }
        public UserControl1(ScriptContext scriptContext)
        {
            SC = scriptContext;
            SS = SC.StructureSet;
            SI = SC.Image;
            SIU = SI.UserOrigin;
            IsoList = new List<double>();

            string[] Basiclines = File.ReadAllLines(@"\\Vmstbox161\va_data$\ProgramData\Vision\PublishedScripts\Machine.csv");
            List<string> sourceMachine = Basiclines[0].Trim().Split(',').Select(s => s.Trim()).ToList();
            MachineName = new List<String>();
            foreach (string line in sourceMachine)
            {
                MachineName.Add(line);
            }

            InitializeComponent();
            DataContext = this;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SC.Patient.BeginModifications();
            Course course = SC.Patient.Courses.Where(s => s.Id == "AutoLoad").FirstOrDefault();
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
            if (ss.Id.Length > 9) { PLANID = ss.Id.Substring(0, 8); } else { PLANID = "Load" + ss.Id; }
            ExternalPlanSetup plan = course.ExternalPlanSetups.Where(s => s.Id == PLANID).FirstOrDefault();
            if (plan is null)
            {
                plan = course.AddExternalPlanSetup(ss);
                plan.Id = PLANID;
            }

            double chkOrientation = new double();
            PatientOrientation orientation = SC.Image.ImagingOrientation;
            if (orientation == PatientOrientation.HeadFirstSupine | orientation == PatientOrientation.FeetFirstSupine | orientation == PatientOrientation.Sitting) chkOrientation = 1;
            else if (orientation == PatientOrientation.HeadFirstProne | orientation == PatientOrientation.FeetFirstProne) chkOrientation = -1;
            else System.Windows.Forms.MessageBox.Show("This CT image Orientation is not supported : No Orientation or Decubitus");

            SC.Patient.BeginModifications();
            StructureSet SS = SC.StructureSet;
            if (SS is null)
            {
                SS = SI.CreateNewStructureSet();
                SS.Id = SI.Id;
            }

            //Find center X
            double originX = SI.UserOrigin.x;//SI.Origin.x + (SI.XRes * SI.XSize / 2);
            double originY = SI.UserOrigin.y;//SI.Origin.y + chkOrientation*(SI.YRes * SI.YSize / 2);
            double originZ = SI.UserOrigin.z; //SI.Origin.z + (SI.ZRes * SI.ZSize / 2);
            VVector Start = new VVector(originX + 700, originY, originZ);
            VVector Stop = new VVector(originX - 700, originY, originZ);
            double[] PreallocatedBuffer = new double[1000];
            ImageProfile XProfile = SI.GetImageProfile(Start, Stop, PreallocatedBuffer);
            double X2 = XProfile.Where(p => !Double.IsNaN(p.Value)).Max(p => p.Position.x);
            double X1 = XProfile.Where(p => !Double.IsNaN(p.Value)).Min(p => p.Position.x);
            double Xcenter = (X2 + X1) / 2;
            double Xborder = Math.Abs(X2 - X1);

            //Find center Y
            Start = new VVector(Xcenter, 700 * chkOrientation + originY, originZ);
            Stop = new VVector(Xcenter, -700 * chkOrientation + originY, originZ);
            double[] YPreallocatedBuffer = new double[1000];
            ImageProfile YProfile = SI.GetImageProfile(Start, Stop, YPreallocatedBuffer);
            double Y2 = YProfile.Where(p => !Double.IsNaN(p.Value)).Max(p => p.Position.y);
            double Y1 = YProfile.Where(p => !Double.IsNaN(p.Value)).Min(p => p.Position.y);
            double Ycenter = (Y2 + Y1) / 2;
            double Yborder = Math.Abs(X2 - X1);

            //chkBrain
            bool chkBrain, chkBrain2 = new bool(); chkBrain2 = false;
            double BrainBorder1 = YProfile.Where(p => !Double.IsNaN(p.Value) && p.Value != -1000).Min(p => p.Position.y);
            double BrainBorder2 = YProfile.Where(p => !Double.IsNaN(p.Value) && p.Value != -1000).Max(p => p.Position.y);
            if (Math.Abs(BrainBorder1 - BrainBorder2) <= 500) chkBrain = true; else chkBrain = false;

            //Find Y line edge near 53cm
            List<double> YHU_Diff = new List<double>();
            List<double> YLocation = new List<double>();
            VVector __Start = new VVector();
            VVector __Stop = new VVector();
            int a = 1; if (chkBrain == true) a = Convert.ToInt32(BrainBorder1);
            double sum = new double();
            for (int i = a; i < Convert.ToInt32(SI.YSize * SI.YRes / 2); i++)
            {

                __Start = new VVector(X1, chkOrientation * ((SI.YSize * SI.YRes / 2) - (i)) + Ycenter, originZ);//(-SI.XSize * SI.XRes / 2) + Xcenter
                __Stop = new VVector(X2, chkOrientation * ((SI.YSize * SI.YRes / 2) - (i)) + Ycenter, originZ);
                double[] __PreallocatedBuffer = new double[1000];
                ImageProfile __Profile = SI.GetImageProfile(__Start, __Stop, __PreallocatedBuffer);
                sum = 0;
                if (chkBrain == true)
                {
                    foreach (ProfilePoint x in __Profile.Where(p => !Double.IsNaN(p.Value) && (p.Value != -1000)))
                    {
                        sum += Math.Abs(x.Value - (-450));
                    }
                }
                else
                {
                    foreach (ProfilePoint x in __Profile.Where(p => !Double.IsNaN(p.Value)))
                    {
                        sum += Math.Abs(x.Value - (-450));
                    }
                }

                if (sum != 0)
                {
                    YHU_Diff.Add(sum);
                    YLocation.Add(chkOrientation * ((SI.YSize * SI.YRes / 2) - (i)) + Ycenter);
                }
            }
            int index = new int();
            double FinalYcenter, chkHeight, Brn1, Brn2, Brn3, Brn4, BodyfixChk = new double();
            List<double> BadChk = new List<double>();
            double[] _PreallocatedBuffer = new double[1000];
            double[] _PreallocatedBuffer1 = new double[100];

            //Find the point with the highest slope from centerx, and check the distance near 47cm or 51cm
            VVector Couch1, Couch2, Couch3, Couch4, _Start, _Stop = new VVector();
            index = YHU_Diff.IndexOf(YHU_Diff.Min());
            FinalYcenter = YLocation.ElementAt(index);
            double limit1 = FinalYcenter + chkOrientation * 5;
            double limit2 = FinalYcenter - chkOrientation * 5;

            for (int i = 0; i < 50; i++)
            {
                _Start = new VVector(-275 + Xcenter, FinalYcenter, originZ);
                _Stop = new VVector(0 + Xcenter, FinalYcenter, originZ);
                ImageProfile XProfile3 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer);
                Couch3 = FindHighestSlope(XProfile3);
                _Start = new VVector(0 + Xcenter, FinalYcenter, originZ);
                _Stop = new VVector(275 + Xcenter, FinalYcenter, originZ);
                ImageProfile XProfile4 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer);
                Couch4 = FindHighestSlope(XProfile4);
                limit1 = FinalYcenter + chkOrientation * 5; limit2 = FinalYcenter - chkOrientation * 5;
                if (limit1 > limit2) { limit2 = limit1; limit1 = FinalYcenter - chkOrientation * 5; }

                if ((limit2 < Y2) && (limit1 > Y1))
                {
                    _Start = new VVector(-275 + Xcenter, FinalYcenter + chkOrientation * 3, originZ);
                    _Stop = new VVector(0 + Xcenter, FinalYcenter + chkOrientation * 3, originZ);
                    ImageProfile XProfile1 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer);
                    Couch1 = FindHighestSlope(XProfile1);
                    _Start = new VVector(0 + Xcenter, FinalYcenter + chkOrientation * 3, originZ);
                    _Stop = new VVector(275 + Xcenter, FinalYcenter + chkOrientation * 3, originZ);
                    ImageProfile XProfile2 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer);
                    Couch2 = FindHighestSlope(XProfile2);

                    _Start = new VVector(-50 + Xcenter, FinalYcenter + chkOrientation, originZ);
                    _Stop = new VVector(50 + Xcenter, FinalYcenter + chkOrientation, originZ);
                    ImageProfile XProfilechk = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer1);
                    chkHeight = XProfilechk[50].Value;

                    _Start = new VVector(-50 + Xcenter, FinalYcenter + chkOrientation * 5, originZ);
                    _Stop = new VVector(50 + Xcenter, FinalYcenter + chkOrientation * 5, originZ);
                    ImageProfile XProfileBrn1 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer1);
                    Brn1 = XProfileBrn1.Where(p => !Double.IsNaN(p.Value)).Min(p => p.Value);

                    _Start = new VVector(-250 + Xcenter, FinalYcenter - chkOrientation * 5, originZ);
                    _Stop = new VVector(250 + Xcenter, FinalYcenter - chkOrientation * 5, originZ);
                    ImageProfile XProfileBrn2 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer1);
                    Brn2 = XProfileBrn2.Where(p => p.Value != -1024).Where(p => p.Value != -1000).Where(p => !Double.IsNaN(p.Value)).Min(p => p.Value);

                    _Start = new VVector(-50 + Xcenter, FinalYcenter + chkOrientation * 4, originZ);
                    _Stop = new VVector(50 + Xcenter, FinalYcenter + chkOrientation * 4, originZ);
                    ImageProfile XProfileBrn3 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer1);
                    Brn3 = XProfileBrn3.Where(p => !Double.IsNaN(p.Value)).Min(p => p.Value);

                    _Start = new VVector(-250 + Xcenter, FinalYcenter - chkOrientation * 4, originZ);
                    _Stop = new VVector(250 + Xcenter, FinalYcenter - chkOrientation * 4, originZ);
                    ImageProfile XProfileBrn4 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer1);
                    Brn4 = XProfileBrn4.Where(p => p.Value != -1024).Where(p => p.Value != -1000).Where(p => !Double.IsNaN(p.Value)).Min(p => p.Value);

                    _Start = new VVector(0 + Xcenter, FinalYcenter + chkOrientation * 8.5, originZ);
                    _Stop = new VVector(250 + Xcenter, FinalYcenter + chkOrientation * 8.5, originZ);
                    ImageProfile XProfileBrn5 = SI.GetImageProfile(_Start, _Stop, _PreallocatedBuffer1);
                    BodyfixChk = XProfileBrn5[99].Value;

                    if (Brn1 < -600 && Brn2 < -600 && (Brn3 <= -850 && Brn3 >= -950) && Brn4 < -600)
                    { chkBrain2 = true; }

                    double CouchBorder1 = Math.Round(VVector.Distance(Couch1, Couch2) / 10);
                    double CouchBorder2 = Math.Round(VVector.Distance(Couch3, Couch4) / 10);
                    if (CouchBorder1 < CouchBorder2)
                    {
                        CouchBorder1 = Math.Round(VVector.Distance(Couch3, Couch4) / 10);
                        CouchBorder2 = Math.Round(VVector.Distance(Couch1, Couch2) / 10);
                    }
                    if (((CouchBorder1 >= 49 && CouchBorder1 <= 54) && (CouchBorder2 >= 47 && CouchBorder2 <= 54) && (chkHeight > -650 | chkBrain2 == true) && BodyfixChk < 0) | (chkBrain == true && chkBrain2 == true)) break;
                    YHU_Diff.RemoveAt(index);
                    YLocation.RemoveAt(index);
                }
                else
                {
                    YHU_Diff.RemoveAt(index);
                    YLocation.RemoveAt(index);
                }
                index = YHU_Diff.IndexOf(YHU_Diff.Min());
                FinalYcenter = YLocation.ElementAt(index);
                i++;
            }
            //Add Couch
            FinalYcenter = FinalYcenter - 0.4 * chkOrientation;
            bool imageResized = true;
            string errorCouch = "error";
            Structure BODY = SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL");
            double BodyVolume = new double();
            if (BODY == null)
            {
                var BodyPar = SS.GetDefaultSearchBodyParameters();
                BodyPar.KeepLargestParts = true;
                SS.CreateAndSearchBody(BodyPar);
                BodyVolume = SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL").Volume;
                SS.RemoveStructure(SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL"));
                BodyPar = SS.GetDefaultSearchBodyParameters();
                //NTUH default setting
                BodyPar.LowerHUThreshold = -350;
                BodyPar.KeepLargestParts = false;
                BodyPar.PreDisconnect = false;
                BodyPar.FillAllCavities = true;
                BodyPar.PreCloseOpenings = true;
                BodyPar.PreCloseOpeningsRadius = 0.2;
                BodyPar.Smoothing = true;
                BodyPar.SmoothingLevel = 3;
                SS.CreateAndSearchBody(BodyPar);
            }
            else if (BODY.Volume == 0)
            {
                var BodyPar = SS.GetDefaultSearchBodyParameters();
                BodyPar.KeepLargestParts = true;
                SS.CreateAndSearchBody(BodyPar);
                BodyVolume = SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL").Volume;
                SS.RemoveStructure(SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL"));
                BodyPar = SS.GetDefaultSearchBodyParameters();
                //NTUH default setting
                BodyPar.LowerHUThreshold = -350;
                BodyPar.KeepLargestParts = false;
                BodyPar.PreDisconnect = false;
                BodyPar.FillAllCavities = true;
                BodyPar.PreCloseOpenings = true;
                BodyPar.PreCloseOpeningsRadius = 0.2;
                BodyPar.Smoothing = true;
                BodyPar.SmoothingLevel = 3;
                BodyPar.KeepLargestParts = false;
                SS.CreateAndSearchBody(BodyPar);
            }
            List<VVector> CSVVector = new List<VVector>();
            if (XamlAddCouch == true)
            {
                bool AddCouch = true;
                if (SI.XSize * SI.XRes <= 540)
                {
                    DialogResult result = System.Windows.Forms.MessageBox.Show("Enlarging is irreversible. Are you sure you want to enlarge the image?", "External Beam Planning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) { AddCouch = false; }
                }
                switch (AddCouch)
                {
                    case (false):
                        break;
                    default:
                        if (SS.CanAddCouchStructures(out errorCouch) == true)
                        {
                            SS.AddCouchStructures("Exact_IGRT_Couch_Top_medium", orientation, RailPosition.In, RailPosition.In, -500, -950, null, out IReadOnlyList<Structure> couchStructureList, out imageResized, out errorCouch);
                            Structure CouchSurface = SS.Structures.FirstOrDefault(s => s.Id == "CouchSurface");
                            Structure CouchInterior = SS.Structures.FirstOrDefault(s => s.Id == "CouchInterior");
                            StructureCode CScode = CouchSurface.StructureCode;
                            StructureCode CIcode = CouchInterior.StructureCode;
                            CouchSurface.SegmentVolume = CouchSurface.SegmentVolume.Or(CouchInterior.SegmentVolume);
                            foreach (VVector[] vectors in CouchSurface.GetContoursOnImagePlane(1))
                            {
                                foreach (VVector v in vectors)
                                {
                                    double x = v.x;
                                    double y = v.y;
                                    double z = v.z;
                                    CSVVector.Add(new VVector(x, y, z));
                                }
                            }
                            double MMX = MaxMinDetect(CSVVector, orientation)[0]; double MMY = MaxMinDetect(CSVVector, orientation)[1];
                            double ShiftX = -265 - MMX;
                            double ShiftY = (FinalYcenter) - MMY;

                            SS.RemoveStructure(CouchSurface);
                            CouchSurface = SS.AddStructure("SUPPORT", "CouchSurface");
                            for (int i = 0; i < Convert.ToInt32(SI.ZSize); i++)
                            {
                                CouchSurface.AddContourOnImagePlane(CSVVector.Select(v => new VVector(v.x + ShiftX, v.y + ShiftY, v.z)).ToArray(), i);
                            }


                            CSVVector.Clear();
                            foreach (VVector[] vectors in CouchInterior.GetContoursOnImagePlane(1))
                            {
                                foreach (VVector v in vectors)
                                {
                                    double x = v.x;
                                    double y = v.y;
                                    double z = v.z;
                                    CSVVector.Add(new VVector(x, y, z));
                                }
                            }
                            SS.RemoveStructure(CouchInterior);
                            CouchInterior = SS.AddStructure("SUPPORT", "CouchInterior");
                            for (int i = 0; i < Convert.ToInt32(SI.ZSize); i++)
                            {
                                CouchInterior.AddContourOnImagePlane(CSVVector.Select(v => new VVector(v.x + ShiftX, v.y + ShiftY, v.z)).ToArray(), i);
                            }
                            CouchSurface.SegmentVolume = CouchSurface.SegmentVolume.Sub(CouchInterior.SegmentVolume);
                            //CouchInterior.SegmentVolume = CouchInterior.AsymmetricMargin(new AxisAlignedMargins(StructureMarginGeometry.Outer, 0,0,0,0, 0.03, 0));
                            CouchInterior.SetAssignedHU(-950);
                            CouchSurface.SetAssignedHU(-550);
                            CouchInterior.Comment = "NTUH_Exact IGRT Couch, medium";
                            CouchSurface.Comment = "NTUH_Exact IGRT Couch, medium";
                            CouchSurface.StructureCode = CScode;
                            CouchInterior.StructureCode = CIcode;

                            //BODY part
                            BODY = SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL");
                            Structure Temp = SS.AddStructure("CONTROL", "Temp_ForCouch");
                            VVector[] TempVec = GetpseudoLine(FinalYcenter, SI.XSize, SI.YSize, chkOrientation);
                            for (int i = 0; i < Convert.ToInt32(SI.ZSize); i++)
                            {
                                Temp.AddContourOnImagePlane(TempVec, i);
                            }
                            BODY.SegmentVolume = BODY.SegmentVolume.Sub(Temp.SegmentVolume);
                            SS.RemoveStructure(Temp);
                            if (BODY.Volume > BodyVolume) { System.Windows.Forms.MessageBox.Show("Please Check your BODY carefully", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                            BODY.Comment = "Modified by ESAPI";
                        }
                        else if (errorCouch.Contains("Support structures already exist in the structure set."))
                        {
                            Structure Encompass = SS.Structures.FirstOrDefault(s => s.Id == "Encompass");
                            Structure EncompassBase = SS.Structures.FirstOrDefault(s => s.Id == "Encompass Base");
                            if (EncompassBase == null)
                            {
                                CSVVector.Clear();
                                Structure CouchSurface = SS.Structures.FirstOrDefault(s => s.Id == "CouchSurface");
                                foreach (VVector[] vectors in CouchSurface.GetContoursOnImagePlane(1))
                                {
                                    foreach (VVector v in vectors)
                                    {
                                        double x = v.x;
                                        double y = v.y;
                                        double z = v.z;
                                        CSVVector.Add(new VVector(x, y, z));
                                    }
                                }
                                if (chkOrientation == 1)
                                { FinalYcenter = CSVVector.Min(p => p.y); }
                                else { FinalYcenter = CSVVector.Max(p => p.y); }
                                BODY = SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL");
                                Structure Temp = SS.AddStructure("CONTROL", "Temp_ForCouch");
                                VVector[] TempVec = GetpseudoLine(FinalYcenter, SI.XSize, SI.YSize, chkOrientation);
                                for (int i = 0; i < Convert.ToInt32(SI.ZSize); i++)
                                {
                                    Temp.AddContourOnImagePlane(TempVec, i);
                                }
                                BODY.SegmentVolume = BODY.SegmentVolume.Sub(Temp.SegmentVolume);
                                SS.RemoveStructure(Temp);
                                if (BODY.Volume > BodyVolume) { System.Windows.Forms.MessageBox.Show("Please Check your BODY carefully", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                                BODY.Comment = "Modified by ESAPI";
                            }
                            else
                            {
                                CSVVector.Clear();
                                for (int i = 0; i < Convert.ToInt32(SI.ZSize); i++)
                                {
                                    foreach (VVector[] vectors in EncompassBase.GetContoursOnImagePlane(i))
                                    {
                                        foreach (VVector v in vectors)
                                        {
                                            double x = v.x;
                                            double y = v.y;
                                            double z = v.z;
                                            CSVVector.Add(new VVector(x, y, z));
                                        }
                                    }
                                }
                                FinalYcenter = CSVVector.Min(p => p.y);
                                SS.RemoveStructure(SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL"));
                                var BodyPar = SS.GetDefaultSearchBodyParameters();
                                BodyPar.LowerHUThreshold = -700;
                                BodyPar.Smoothing = true;
                                BodyPar.SmoothingLevel = 1;
                                //NTUH default setting
                                BodyPar.KeepLargestParts = false;
                                //BodyPar.PreDisconnect = true;
                                //BodyPar.PreDisconnectRadius = 0.2;
                                //BodyPar.FillAllCavities = true;
                                //BodyPar.PreCloseOpenings = true;
                                //BodyPar.PreCloseOpeningsRadius = 0.2;
                                //BodyPar.Smoothing = true;
                                //BodyPar.SmoothingLevel = 3;
                                SS.CreateAndSearchBody(BodyPar);
                                Structure Temp = SS.AddStructure("CONTROL", "Temp_ForCouch");
                                VVector[] TempVec = GetpseudoLine(FinalYcenter, SI.XSize, SI.YSize, chkOrientation);
                                for (int i = 0; i < Convert.ToInt32(SI.ZSize); i++)
                                {
                                    Temp.AddContourOnImagePlane(TempVec, i);
                                }
                                BODY = SS.Structures.FirstOrDefault(s => s.DicomType == "EXTERNAL");
                                BODY.SegmentVolume = BODY.SegmentVolume.Sub(Temp.SegmentVolume);
                                BODY.SegmentVolume = BODY.SegmentVolume.Sub(Encompass.SegmentVolume);
                                BODY.SegmentVolume = BODY.SegmentVolume.Sub(EncompassBase.SegmentVolume);
                                SS.RemoveStructure(Temp);
                                if (BODY.Volume > BodyVolume) { System.Windows.Forms.MessageBox.Show("Please Check your BODY carefully", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                                BODY.Comment = "Modified by ESAPI";
                                //NTUCC
                                foreach (Structure st in SS.Structures.Where(s => s.DicomType == "GTV"))
                                {
                                    if (st.CanConvertToHighResolution() == true)
                                    {
                                        st.ConvertToHighResolution();
                                    }
                                }
                                foreach (Structure st in SS.Structures.Where(s => s.DicomType == "CTV"))
                                {
                                    if (st.CanConvertToHighResolution() == true)
                                    {
                                        st.ConvertToHighResolution();
                                    }
                                }
                                foreach (Structure st in SS.Structures.Where(s => s.DicomType == "PTV"))
                                {
                                    if (st.CanConvertToHighResolution() == true)
                                    {
                                        st.ConvertToHighResolution();
                                    }
                                }
                            }
                        }
                        else { System.Windows.MessageBox.Show(errorCouch); }
                        break;
                }
            }
            else if (XamlAddCouch == false)
            {
                if (SS.CanAddCouchStructures(out errorCouch) == true)
                {
                    SS.AddCouchStructures("Exact_IGRT_Couch_Top_medium", orientation, RailPosition.In, RailPosition.In, -500, -950, null, out IReadOnlyList<Structure> couchStructureList, out imageResized, out errorCouch);
                }
            }
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
            { beam.CreateOrReplaceDRR(myDRR); }

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
                MarkerDescript += "\nIsocenter" + a + "\t\tx:" + Math.Round((Iso.CenterPoint.x - SIU.x) / 10, 2) + "\ty:" + Math.Round((Iso.CenterPoint.y - SIU.y) / 10, 2) + "\tz:" + Math.Round((Iso.CenterPoint.z - SIU.z) / 10, 2);
                a = a + 1;
            }
            System.Windows.Forms.MessageBox.Show("You will Apply the following Beams\n------------------------------------------------------" + MarkerDescript);
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            string MarkerDescript = string.Empty;
            System.Windows.Forms.MessageBox.Show("You will NOT apply multiple isocenters");
        }
        private void CheckBox_Unchecked_Couch(object sender, RoutedEventArgs e)
        {
            XamlAddCouch = false;
        }

        private void CheckBox_Checked_Couch(object sender, RoutedEventArgs e)
        {
            XamlAddCouch = true;
        }
        public static VVector FindHighestSlope(ImageProfile collection)
        {
            VVector HighestSlope = new VVector();
            var minDifference = double.MinValue;
            for (int i = 1; i < collection.Count() - 1; i++)
            {
                var difference = Math.Abs((long)collection[i + 1].Value - collection[i].Value);
                if (difference > minDifference)
                {
                    minDifference = (double)difference;
                    HighestSlope = collection[i].Position;
                }
            }
            return HighestSlope;
        }

        public static VVector[] GetpseudoLine(double yPlane, double Xsize, double Ysize, double chkorientation)
        {
            List<VVector> vvectors = new List<VVector>();
            vvectors.Add(new VVector(-Xsize, yPlane, 0));//20230616 NTUH from -30*reverse change to 0 
            vvectors.Add(new VVector(Xsize, yPlane, 0));
            vvectors.Add(new VVector(Xsize, chkorientation * Ysize, 0));
            vvectors.Add(new VVector(-Xsize, chkorientation * Ysize, 0));
            return vvectors.ToArray();
        }
        public double[] MaxMinDetect(List<VVector> VVectors, PatientOrientation Ori)
        {
            double[] Final = { VVectors[0].x, VVectors[0].y, VVectors[0].z };
            for (int i = 1; i < VVectors.Count(); i++)
            {
                Final[0] = Math.Min(VVectors[i].x, Final[0]); //Always get the maximum value
                if (Ori == PatientOrientation.HeadFirstSupine | Ori == PatientOrientation.FeetFirstSupine)
                {
                    Final[1] = Math.Min(VVectors[i].y, Final[1]);
                }
                else if (Ori == PatientOrientation.HeadFirstProne | Ori == PatientOrientation.FeetFirstProne)
                {
                    Final[1] = Math.Max(VVectors[i].y, Final[1]);
                }
                Final[2] = Math.Min(VVectors[i].z, Final[2]);
            }
            return Final;
        }
    }
}
