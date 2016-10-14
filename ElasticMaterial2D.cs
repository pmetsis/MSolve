using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.PreProcessor.Interfaces;
using ISAAR.MSolve.Matrices.Interfaces;
using ISAAR.MSolve.Matrices;

namespace ISAAR.MSolve.PreProcessor.Materials
{
    public class ElasticMaterial2D : IFiniteElementMaterial2D
    {
        private readonly double[] strains = new double[3];
        private readonly double[] stresses = new double[3];
        private double[,] constitutiveMatrix = null;
        public double YoungModulus { get; set; }
        public double PoissonRatio { get; set; }
        public double[] Coordinates { get; set; }

        private double[,] GetConstitutiveMatrix()
        {

            //Panos Plane Stress
            //
            //                    [ 1       v       0   ]
            // [D] = E/(1-v^2)    [ v       1       0   ]
            //                    [ 0       0   (1-v)/2 ]
            //

            double fE1 = YoungModulus / (1 - PoissonRatio*PoissonRatio);
            double fE2 = (1 - PoissonRatio)/2;
            double[,] afE = new double[3, 3];
            afE[0, 0] = fE1;
            afE[0, 1] = fE1 * PoissonRatio;
            //afE[0, 2] = 0;
            afE[1, 0] = fE1 * PoissonRatio;
            afE[1, 1] = fE1;
            //afE[1, 2] = 0;
            //afE[2, 0] = 0;
            //afE[2, 1] = 0;
            afE[2, 2] = fE1*fE2;


            Vector<double> s = (new Matrix2D<double>(afE)) * (new Vector<double>(strains));
            s.Data.CopyTo(stresses, 0);

            return afE;
        }

        #region IFiniteElementMaterial Members

        public int ID
        {
            get { return 1; }
        }

        public bool Modified
        {
            get { return false; }
        }

        public void ResetModified()
        {
        }

        #endregion

        #region IFiniteElementMaterial3D Members

        public double[] Stresses { get { return stresses; } }

        public IMatrix2D<double> ConstitutiveMatrix
        {
            get
            {
                if (constitutiveMatrix == null) UpdateMaterial(new double[3]);
                return new Matrix2D<double>(constitutiveMatrix);
            }
        }

        public void UpdateMaterial(double[] strains)
        {
            //throw new NotImplementedException();

            strains.CopyTo(this.strains, 0);
            constitutiveMatrix = GetConstitutiveMatrix();
        }

        public void ClearState()
        {
            //throw new NotImplementedException();
        }

        public void SaveState()
        {
            //throw new NotImplementedException();
        }

        public void ClearStresses()
        {
            //throw new NotImplementedException();
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return new ElasticMaterial2D() { YoungModulus = this.YoungModulus, PoissonRatio = this.PoissonRatio };
        }

        #endregion

    }
}
