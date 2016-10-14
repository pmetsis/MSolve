using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.PreProcessor.Embedding;
using ISAAR.MSolve.PreProcessor.Interfaces;
using ISAAR.MSolve.Matrices.Interfaces;
using System.Runtime.InteropServices;
using ISAAR.MSolve.Matrices;
using ISAAR.MSolve.PreProcessor.Elements.SupportiveClasses;

namespace ISAAR.MSolve.PreProcessor.Elements
{
	public class Quad4 : IStructuralFiniteElement, IEmbeddedHostElement
	{
		protected static double determinantTolerance = 0.00000001;

		protected static int iInt = 2;
		protected static int iInt2 = iInt * iInt;
		protected static int iInt3 = iInt2 * iInt;
        protected readonly static DOFType[] nodalDOFTypes = new DOFType[] { DOFType.X, DOFType.Y}; //Panos - 2 DoF per node
        protected readonly static DOFType[][] dofTypes = new DOFType[][] { nodalDOFTypes, nodalDOFTypes, 
            nodalDOFTypes,nodalDOFTypes}; //Panos - 4 nodes only
		
		//protected readonly IFiniteElementMaterial3D[] materialsAtGaussPoints; //Panos - What about 2D Material? Implemented
		protected readonly IFiniteElementMaterial2D[] materialsAtGaussPoints; 

		protected IFiniteElementDOFEnumerator dofEnumerator = new GenericDOFEnumerator();
        protected Quad4()
		{
		}
        public Quad4(IFiniteElementMaterial2D material)
		{
			materialsAtGaussPoints = new IFiniteElementMaterial2D[iInt2];
			for (int i = 0; i < iInt2; i++)
				materialsAtGaussPoints[i] = (IFiniteElementMaterial2D)material.Clone();
		}
        public Quad4(IFiniteElementMaterial2D material, IFiniteElementDOFEnumerator dofEnumerator)
            : this(material)
        {
			this.dofEnumerator = dofEnumerator;
		}
        public IFiniteElementDOFEnumerator DOFEnumerator
		{
			get { return dofEnumerator; }
			set { dofEnumerator = value; }
		}
        //Panos - I dont think I need these
		public double Density { get; set; }
		public double RayleighAlpha { get; set; }
		public double RayleighBeta { get; set; }
		//
        protected double[,] GetCoordinates(Element element)
		{
			//double[,] faXYZ = new double[dofTypes.Length, 3];
			double[,] faXY = new double[dofTypes.Length, 2];
            for (int i = 0; i < dofTypes.Length; i++)
			{
				faXY[i, 0] = element.Nodes[i].X;
				faXY[i, 1] = element.Nodes[i].Y;
			}
			return faXY;
		}
        protected double[,] GetCoordinatesTranspose(Element element)
		{
			double[,] faXY = new double[2, dofTypes.Length];
			for (int i = 0; i < dofTypes.Length; i++)
			{
				faXY[0, i] = element.Nodes[i].X;
				faXY[1, i] = element.Nodes[i].Y;
			}
			return faXY;
		}

		#region IElementType Members

		public int ID
		{
			//Change Element ID to random 14? I am not sure why is this needed
			get { return 14; }
		}

		public ElementDimensions ElementDimensions
		{
			get { return ElementDimensions.TwoD; }
		}

		public virtual IList<IList<DOFType>> GetElementDOFTypes(Element element)
		{
			return dofTypes;
		}
        public IList<Node> GetNodesForMatrixAssembly(Element element)
		{
			return element.Nodes;
		}
            
        private double[] CalcQ4Shape(double fXi, double fEta)
        {
		  // QUAD4 ELEMENT
          //
          //            Eta
          //            ^
          //#4(-1,1)    |     #3(1,1)
          //      ._____|_____.
          //      |     |     |
          //      |     |     |
          //      |     |_____|____>Xi (or psi - the same)
          //      |           |
          //      |           |
          //      .___________.
          // 
          //#1(-1,-1)        #2(1,-1)
          //
            const double fSq050 = 0.50; //Panos - I need 0.25 so 0.50*0.50=0.25
            double fXiP = (1.0 + fXi) * fSq050; //(1+psi) - P means Plus
            double fEtaP = (1.0 + fEta) * fSq050; //(1+eta) - P means Plus
            double fXiM = (1.0 - fXi) * fSq050; //(1-psi) - M means Minus
            double fEtaM = (1.0 - fEta) * fSq050; //(1-eta) - M means Minus
            return new double[]
			{
				fXiM * fEtaM, //N1 = .25*(1-psi).*(1-eta); 
				fXiP * fEtaM, //N2 = .25*(1+psi).*(1-eta);
				fXiP * fEtaP, //N3 = .25*(1+psi).*(1+eta);
				fXiM * fEtaP, //N4 = .25*(1-psi).*(1+eta);
			};
		}

        private double[] CalcQ4ShapeFunctionDerivatives(double fXi, double fEta) //Panos - Shape functions Derivatives
        {
					
            const double fN025 = 0.25; //fSqC125 = 0.5; Panos - Should I Keep the same names? I dont multiply here
			double fXiP = (1.0 + fXi) * fN025; //(1+psi) - P means Plus
			double fEtaP = (1.0 + fEta) * fN025; //(1+eta) - P means Plus
			double fXiM = (1.0 - fXi) * fN025; //(1-psi) - M means Minus
			double fEtaM = (1.0 - fEta) * fN025; //(1-eta) - M means Minus

            double[] faDS = new double[8];
			//Panos - I put first with respect to psi(fXi here) and then eta

			faDS[0] = -fEtaM;  //N1,psi = -.25*(1-eta); 
			faDS[1] = fEtaM;   //N2,psi =  .25*(1-eta);
			faDS[2] = fEtaP;   //N3,psi =  .25*(1+eta);
			faDS[3] = -fEtaP;  //N4,psi = -.25*(1+eta);

			faDS[4] = -fXiM;   //N1,eta = -.25*(1-psi);
			faDS[5] = -fXiP;   //N2,eta = -.25*(1+psi);
			faDS[6] = fXiP;   //N3,eta =  .25*(1+psi);
			faDS[7] = fXiM;    //N4,eta =  .25*(1-psi);

			return faDS;
		}

        private double[,] CalcQ4J(double[,] faXY, double[] faDS, double fXi, double fEta) //Panos - Calculate the Jacobian matrix J
        {
            // Jacobian Matrix J
            //                                        [x1  y1]
            //      [ N1,psi  N2,psi  N3,psi  N4,psi] [x2  y2]
            //[J] = [ N1,eta  N2,eta  N3,eta  N4,eta] [x3  y3]
            //                                        [x4  y4]
            //
            //                   [  ]   multiplied  by [  ]     equals [  ]
            //                      2x4                   4x2             2x2

            // REMINDER faXY[i, 0] = element.Nodes[i].X;
            //          faXY[i, 1] = element.Nodes[i].Y;

            double[,] faJ = new double[2, 2];
            faJ[0, 0] = faDS[0] * faXY[0, 0] + faDS[1] * faXY[1, 0] + faDS[2] * faXY[2, 0] + faDS[3] * faXY[3, 0];
            faJ[0, 1] = faDS[0] * faXY[0, 1] + faDS[1] * faXY[1, 1] + faDS[2] * faXY[2, 1] + faDS[3] * faXY[3, 1];
            faJ[1, 0] = faDS[4] * faXY[0, 0] + faDS[5] * faXY[1, 0] + faDS[6] * faXY[2, 0] + faDS[7] * faXY[3, 0];
            faJ[1, 1] = faDS[4] * faXY[0, 1] + faDS[5] * faXY[1, 1] + faDS[6] * faXY[2, 1] + faDS[7] * faXY[3, 1];

            return faJ;
        }
        private double CalcQ4JDetJ(double[,] faXY, double fXi, double fEta) //Calculate the determinant of the Jacobian Matrix
        {
            //|J| or det[j] = 1/8 [x1 x2 x3 x4] [   0      1-eta    eta-psi    psi-1 ]  [y1]
            //                                  [ eta-1      0      psi+1    -psi-eta]  [y2]
            //                                  [psi-eta   -psi-1     0        eta+1 ]  [y3]
            //                                  [ 1-psi    psi+eta  -eta-1       0   ]  [y4]

            const double fN0125 = 0.125; //=1/8

            double fDetJ = (faXY[0, 0] * ( (1 - fEta) * faXY[1, 1] + (fEta - fXi) * faXY[2, 1] + (fXi - 1) * faXY[3, 1])) +
                           (faXY[1, 0] * ((fEta - 1) * faXY[0, 1] + (fXi + 1) * faXY[2, 1] + (-fXi - fEta) * faXY[3, 1])) +
                           (faXY[2, 0] * ((fXi - fEta) * faXY[0, 1] + (-fXi - 1) * faXY[1, 1] + (fEta + 1) * faXY[3, 1])) +
                           (faXY[3, 0] * ((1 - fXi) * faXY[0, 1] + (fXi + fEta) * faXY[1, 1] + (-fEta - 1) * faXY[2, 1]));

            fDetJ = fDetJ * fN0125;

            if (fDetJ < determinantTolerance)
                throw new ArgumentException(String.Format("Jacobian determinant is negative or under tolerance ({0} < {1})." +
                                                          "Check the order of nodes or the element geometry.", fDetJ, determinantTolerance));
            return fDetJ;
        }
            private double[,] CalcQ4JInv(double fDetJ, double[,] faJ) //Calculate the full inverse of the Jacobian Matrix
        {
            //
            //[J]^-1 = 1/det[J]  [ J22    -J12]
            //                   1[-J21     J11]

            double fDetInv = 1.0 / fDetJ;

			double[,] faJInv = new double[2, 2];
            faJInv[0, 0] = (faJ[1, 1]) * fDetInv; //Panos - prosoxi stin arithmisi to J11 einai faJ[0,0];
		    faJInv[0, 1] = (-faJ[0, 1]) * fDetInv;
			faJInv[1, 0] = (-faJ[1, 0]) * fDetInv;
			faJInv[1, 1] = (faJ[0, 0]) * fDetInv;

            return faJInv;
		}

        private double[,] CalculateDeformationMatrix(double[,] faXY, double[] faDS, double fXi, double fEta, double fDetJ) //Panos - Calculate Deformation matrix B
        {

            //  [B] = 1/|J| [B1 B2 B3 B4]
            //
            //
            //   where  [Bi] = [ a*Ni,psi - b*Ni,eta                  0      ]
            //
            //                 [      0                  c*Ni,eta - d*Ni,psi ]
            //
            //                 [ c*Ni,eta - d*Ni,psi      a*Ni,psi - b*Ni,eta]
            //
            //
            //   where  a=1/4 {y1*(psi-1) + y2*(-1-psi) + y3*(1+psi) + y4*(1-psi)}
            //          b=1/4 {y1*(eta-1) + y2*(1-eta) + y3*(1+eta) + y4*(-1-eta)}
            //          c=1/4 {x1*(eta-1) + x2*(1-eta) + x3*(1+eta) + x4*(-1-eta)}
            //          d=1/4 {x1*(psi-1) + x2*(-1-psi) + x3*(1+psi) + x4*(1-psi)}
            //
            //
            // REMINDER
            // faDS[0] =  N1,psi        faDS[4] = N1,eta 
            // faDS[1] =  N2,psi        faDS[5] = N2,eta 
            // faDS[2] =  N3,psi        faDS[6] = N3,eta 
            // faDS[3] =  N4,psi        faDS[7] = N4,eta 

            double fDetInv = 1.0 / fDetJ; //Panos - SOS, edo einai mono to klasma 1/det[j] kai oxi o full antistrofos pinakas;
            double Aparameter;
            double Bparameter;
            double Cparameter;
            double Dparameter;

            Aparameter = 0.25 * (faXY[0, 1] * (fXi - 1) + faXY[1, 1] * (-1 - fXi) + faXY[2, 1] * (1 + fXi) + faXY[3, 1] * (1 - fXi));
            Bparameter = 0.25 * (faXY[0, 1] * (fEta - 1) + faXY[1, 1] * (1 - fEta) + faXY[2, 1] * (1 + fEta) + faXY[3, 1] * (-1 - fEta));
            Cparameter = 0.25 * (faXY[0, 0] * (fEta - 1) + faXY[1, 0] * (1 - fEta) + faXY[2, 0] * (1 + fEta) + faXY[3, 0] * (-1 - fEta));
            Dparameter = 0.25 * (faXY[0, 0] * (fXi - 1) + faXY[1, 0] * (-1 - fXi) + faXY[2, 0] * (1 + fXi) + faXY[3, 0] * (1 - fXi));

            double[,] Bmatrix = new double[3, 8];

            Bmatrix[0, 0] = fDetInv * (Aparameter * faDS[0] - Bparameter * faDS[4]);
            Bmatrix[1, 0] = 0;
            Bmatrix[2, 0] = fDetInv * (Cparameter * faDS[4] - Dparameter * faDS[0]);
            Bmatrix[0, 1] = 0;
            Bmatrix[1, 1] = fDetInv * (Cparameter * faDS[4] - Dparameter * faDS[0]);
            Bmatrix[2, 1] = fDetInv * (Aparameter * faDS[0] - Bparameter * faDS[4]);

            Bmatrix[0, 2] = fDetInv * (Aparameter * faDS[1] - Bparameter * faDS[5]);
            Bmatrix[1, 2] = 0;
            Bmatrix[2, 2] = fDetInv * (Cparameter * faDS[5] - Dparameter * faDS[1]);
            Bmatrix[0, 3] = 0;
            Bmatrix[1, 3] = fDetInv * (Cparameter * faDS[5] - Dparameter * faDS[1]);
            Bmatrix[2, 3] = fDetInv * (Aparameter * faDS[1] - Bparameter * faDS[5]);

            Bmatrix[0, 4] = fDetInv * (Aparameter * faDS[2] - Bparameter * faDS[6]);
            Bmatrix[1, 4] = 0;
            Bmatrix[2, 4] = fDetInv * (Cparameter * faDS[6] - Dparameter*faDS[2]);
			Bmatrix[0,5]= 0 ;
			Bmatrix[1,5]= fDetInv * (Cparameter*faDS[6] - Dparameter*faDS[2]);
			Bmatrix[2,5]= fDetInv * (Aparameter*faDS[2] - Bparameter*faDS[6]);

			Bmatrix[0,6]= fDetInv * (Aparameter*faDS[3] - Bparameter*faDS[7]);
			Bmatrix[1,6]= 0 ;
			Bmatrix[2,6]= fDetInv * (Cparameter*faDS[7] - Dparameter*faDS[3]);
			Bmatrix[0,7]= 0 ;
			Bmatrix[1,7]= fDetInv * (Cparameter*faDS[7] - Dparameter*faDS[3]);
			Bmatrix[2,7]= fDetInv * (Aparameter*faDS[3] - Bparameter*faDS[7]);

            return Bmatrix;
			//Bmatrix=fDetInv * Bmatrix; //Panos  - den yparxei o operator fDetInv. * mhtroo??? - To kano analytika...
        }
	
		private GaussLegendrePoint2D[] CalculateGaussMatrices(double[,] nodeCoordinates)
		{
			GaussLegendrePoint1D[] integrationPointsPerAxis =
				GaussQuadrature.GetGaussLegendrePoints(iInt);
			int totalSamplingPoints = (int)Math.Pow(integrationPointsPerAxis.Length, 2);

			GaussLegendrePoint2D[] integrationPoints = new GaussLegendrePoint2D[totalSamplingPoints];

			int counter = -1;
			foreach (GaussLegendrePoint1D pointXi in integrationPointsPerAxis)
			{
				foreach (GaussLegendrePoint1D pointEta in integrationPointsPerAxis)
				{
						counter += 1;
						double xi = pointXi.Coordinate;
						double eta = pointEta.Coordinate;
                      //double[] ShapeFunctions = this.CalcQ4Shape(xi,eta); //Panos - Uncommnent to check Shapefunctions
                        double[] faDS = this.CalcQ4ShapeFunctionDerivatives(xi, eta);
                        double [,] faJ = this.CalcQ4J( nodeCoordinates, faDS,  xi,  eta);
                        double fDetJ = this.CalcQ4JDetJ(nodeCoordinates, xi, eta);
                      //double[,] faJInv = this.CalcQ4JInv(fDetJ, faJ); //Panos - Uncommnent to check JInv
					    double [,] deformationMatrix = this.CalculateDeformationMatrix(nodeCoordinates,faDS, xi, eta, fDetJ);
                        double weightFactor = pointXi.WeightFactor * pointEta.WeightFactor * fDetJ; //Panos - we should also insert the thickness of the element t. Now it is assumed t=1;
                        integrationPoints[counter] = new GaussLegendrePoint2D(xi, eta, deformationMatrix, weightFactor);
				}
			}
            return integrationPoints;
		}

		public virtual IMatrix2D<double> StiffnessMatrix(Element element)
		{
			double[,] coordinates = this.GetCoordinates(element);
			GaussLegendrePoint2D[] integrationPoints = this.CalculateGaussMatrices(coordinates);
            SymmetricMatrix2D<double> stiffnessMatrix = new SymmetricMatrix2D<double>(8);
            int pointId = -1;
			foreach (GaussLegendrePoint2D intPoint in integrationPoints)
			{
				pointId++;
				IMatrix2D<double> constitutiveMatrix = materialsAtGaussPoints[pointId].ConstitutiveMatrix;
				double[,] b = intPoint.DeformationMatrix;
				for (int i = 0; i < 8; i++)
				{
					double[] eb = new double[3];
					for (int iE = 0; iE < 3; iE++)
					{
						eb[iE] = (constitutiveMatrix[iE, 0] * b[0, i]) + (constitutiveMatrix[iE, 1] * b[1, i]) +
								 (constitutiveMatrix[iE, 2] * b[2, i]) ;
					}

					for (int j = i; j < 8; j++)
					{
						double stiffness = (b[0, j] * eb[0]) + (b[1, j] * eb[1]) + (b[2, j] * eb[2]) ;
						stiffnessMatrix[i, j] += stiffness * intPoint.WeightFactor;
					}
				}
			}

			return stiffnessMatrix;
		}

		public IMatrix2D<double> CalculateConsistentMass(Element element)
		{
			double[,] coordinates = this.GetCoordinates(element);
			GaussLegendrePoint2D[] integrationPoints = this.CalculateGaussMatrices(coordinates);

			SymmetricMatrix2D<double> consistentMass = new SymmetricMatrix2D<double>(24);

			foreach (GaussLegendrePoint2D intPoint in integrationPoints)
			{
				double[] shapeFunctionValues = this.CalcQ4Shape(intPoint.Xi, intPoint.Eta);
				double weightDensity = intPoint.WeightFactor * this.Density;
				for (int shapeFunctionI = 0; shapeFunctionI < shapeFunctionValues.Length; shapeFunctionI++)
				{
					for (int shapeFunctionJ = shapeFunctionI; shapeFunctionJ < shapeFunctionValues.Length; shapeFunctionJ++)
					{
						consistentMass[3 * shapeFunctionI, 3 * shapeFunctionJ] += shapeFunctionValues[shapeFunctionI] *
																				  shapeFunctionValues[shapeFunctionJ] *
																				  weightDensity;
					}

					for (int shapeFunctionJ = shapeFunctionI; shapeFunctionJ < shapeFunctionValues.Length; shapeFunctionJ++)
					{
						consistentMass[(3 * shapeFunctionI) + 1, (3 * shapeFunctionJ) + 1] =
							consistentMass[3 * shapeFunctionI, 3 * shapeFunctionJ];

						consistentMass[(3 * shapeFunctionI) + 2, (3 * shapeFunctionJ) + 2] =
							consistentMass[3 * shapeFunctionI, 3 * shapeFunctionJ];
					}
				}
			}

			return consistentMass;
		}

		#endregion

		public virtual IMatrix2D<double> MassMatrix(Element element)
		{
			return CalculateConsistentMass(element);
		}

		public virtual IMatrix2D<double> DampingMatrix(Element element)
		{
			var m = MassMatrix(element);
			m.LinearCombination(new double[] { RayleighAlpha, RayleighBeta }, new IMatrix2D<double>[] { MassMatrix(element), StiffnessMatrix(element) });
			return m;
			//double[] faD = new double[300];
			//return new SymmetricMatrix2D<double>(faD);
		}

		public Tuple<double[], double[]> CalculateStresses(Element element, double[] localDisplacements, double[] localdDisplacements)
		{
			double[,] faXY = GetCoordinates(element);
			double[,] faDS = new double[iInt3, 24];
			double[,] faS = new double[iInt3, 8];
			double[,,] faB = new double[iInt3, 24, 6];
			double[] faDetJ = new double[iInt3];
			double[,,] faJ = new double[iInt3, 3, 3];
			double[] faWeight = new double[iInt3];
			double[,] fadStrains = new double[iInt3, 6];
			double[,] faStrains = new double[iInt3, 6];
		//	CalcQ4GaussMatrices(ref iInt, faXY, faWeight, faS, faDS, faJ, faDetJ, faB);
		//	CalcQ4Strains(ref iInt, faB, localDisplacements, faStrains);
		//	CalcQ4Strains(ref iInt, faB, localdDisplacements, fadStrains);

			double[] dStrains = new double[6];
			double[] strains = new double[6];
			for (int i = 0; i < materialsAtGaussPoints.Length; i++)
			{
				for (int j = 0; j < 6; j++) dStrains[j] = fadStrains[i, j];
				for (int j = 0; j < 6; j++) strains[j] = faStrains[i, j];
				materialsAtGaussPoints[i].UpdateMaterial(dStrains);
			}

			return new Tuple<double[], double[]>(strains, materialsAtGaussPoints[materialsAtGaussPoints.Length - 1].Stresses);
		}

		public double[] CalculateForcesForLogging(Element element, double[] localDisplacements)
		{
			return CalculateForces(element, localDisplacements, new double[localDisplacements.Length]);
		}

		public double[] CalculateForces(Element element, double[] localTotalDisplacements, double[] localdDisplacements)
		{
			//Vector<double> d = new Vector<double>(localdDisplacements.Length);
			//for (int i = 0; i < localdDisplacements.Length; i++) 
			//    //d[i] = localdDisplacements[i] + localTotalDisplacements[i];
			//    d[i] = localTotalDisplacements[i];
			//double[] faForces = new double[24];
			//StiffnessMatrix(element).Multiply(d, faForces);

			double[,] faStresses = new double[iInt3, 6];
			for (int i = 0; i < materialsAtGaussPoints.Length; i++)
				for (int j = 0; j < 6; j++) faStresses[i, j] = materialsAtGaussPoints[i].Stresses[j];

			double[,] faXYZ = GetCoordinates(element);
			double[,] faDS = new double[iInt3, 24];
			double[,] faS = new double[iInt3, 8];
			double[,,] faB = new double[iInt3, 24, 6];
			double[] faDetJ = new double[iInt3];
			double[,,] faJ = new double[iInt3, 3, 3];
			double[] faWeight = new double[iInt3];
			double[] faForces = new double[24];
		//	CalcQ4GaussMatrices(ref iInt, faXYZ, faWeight, faS, faDS, faJ, faDetJ, faB);
		//	CalcQ4Forces(ref iInt, faB, faWeight, faStresses, faForces);

			return faForces;
		}

		public double[] CalculateAccelerationForces(Element element, IList<MassAccelerationLoad> loads)
		{
			Vector<double> accelerations = new Vector<double>(24);
			IMatrix2D<double> massMatrix = MassMatrix(element);

			foreach (MassAccelerationLoad load in loads)
			{
				int index = 0;
				foreach (DOFType[] nodalDOFTypes in dofTypes)
					foreach (DOFType dofType in nodalDOFTypes)
					{
						if (dofType == load.DOF) accelerations[index] += load.Amount;
						index++;
					}
			}
			double[] forces = new double[24];
			massMatrix.Multiply(accelerations, forces);
			return forces;
		}

		public void ClearMaterialState()
		{
			foreach (IFiniteElementMaterial2D m in materialsAtGaussPoints) m.ClearState();
		}

		public void SaveMaterialState()
		{
			foreach (IFiniteElementMaterial2D m in materialsAtGaussPoints) m.SaveState();
		}

		public void ClearMaterialStresses()
		{
			foreach (IFiniteElementMaterial2D m in materialsAtGaussPoints) m.ClearStresses();
		}

		#region IStructuralFiniteElement Members

		public bool MaterialModified
		{
			get
			{
				foreach (IFiniteElementMaterial2D material in materialsAtGaussPoints)
					if (material.Modified) return true;
				return false;
			}
		}

		public void ResetMaterialModified()
		{
			foreach (IFiniteElementMaterial2D material in materialsAtGaussPoints) material.ResetModified();
		}

		#endregion

		#region IEmbeddedHostElement Members

		public EmbeddedNode BuildHostElementEmbeddedNode(Element element, Node node, IEmbeddedDOFInHostTransformationVector transformationVector)
		{
			var points = GetNaturalCoordinates(element, node);
			if (points.Length == 0) return null;

			element.EmbeddedNodes.Add(node);
			var embeddedNode = new EmbeddedNode(node, element, transformationVector.GetDependentDOFTypes);
			for (int i = 0; i < points.Length; i++)
				embeddedNode.Coordinates.Add(points[i]);
			return embeddedNode;
		}

		public double[] GetShapeFunctionsForNode(Element element, EmbeddedNode node)
		{
			double[,] elementCoordinates = GetCoordinatesTranspose(element);
			var shapeFunctions = CalcQ4Shape(node.Coordinates[0], node.Coordinates[1]);
			var ShapeFunctionDerivatives = CalcQ4ShapeFunctionDerivatives(node.Coordinates[0], node.Coordinates[1]);
			var jacobian = CalcQ4J(elementCoordinates, ShapeFunctionDerivatives, node.Coordinates[0], node.Coordinates[1]);

			return new double[]
			{
				shapeFunctions[0], shapeFunctions[1], shapeFunctions[2], shapeFunctions[3],
				ShapeFunctionDerivatives[0], ShapeFunctionDerivatives[1], ShapeFunctionDerivatives[2], ShapeFunctionDerivatives[3], ShapeFunctionDerivatives[4], ShapeFunctionDerivatives[5], ShapeFunctionDerivatives[6], ShapeFunctionDerivatives[7],
				jacobian[0, 0], jacobian[0, 1], jacobian[1, 0], jacobian[1, 1]
			};

			//double[] coords = GetNaturalCoordinates(element, node);
			//return CalcH8Shape(coords[0], coords[1], coords[2]);

			//double fXiP = (1.0 + coords[0]) * 0.5;
			//double fEtaP = (1.0 + coords[1]) * 0.5;
			//double fZetaP = (1.0 + coords[2]) * 0.5;
			//double fXiM = (1.0 - coords[0]) * 0.5;
			//double fEtaM = (1.0 - coords[1]) * 0.5;
			//double fZetaM = (1.0 - coords[2]) * 0.5;

			//return new double[] { fXiM * fEtaM * fZetaM,
			//    fXiP * fEtaM * fZetaM,
			//    fXiP * fEtaP * fZetaM,
			//    fXiM * fEtaP * fZetaM,
			//    fXiM * fEtaM * fZetaP,
			//    fXiP * fEtaM * fZetaP,
			//    fXiP * fEtaP * fZetaP,
			//    fXiM * fEtaP * fZetaP };
		}

		private double[] GetNaturalCoordinates(Element element, Node node)
		{
			double[] mins = new double[] { element.Nodes[0].X, element.Nodes[0].Y};
			double[] maxes = new double[] { element.Nodes[0].X, element.Nodes[0].Y};
			for (int i = 0; i < element.Nodes.Count; i++)
			{
				mins[0] = mins[0] > element.Nodes[i].X ? element.Nodes[i].X : mins[0];
				mins[1] = mins[1] > element.Nodes[i].Y ? element.Nodes[i].Y : mins[1];
				maxes[0] = maxes[0] < element.Nodes[i].X ? element.Nodes[i].X : maxes[0];
				maxes[1] = maxes[1] < element.Nodes[i].Y ? element.Nodes[i].Y : maxes[1];
			}
            //return new double[] { (node.X - mins[0]) / ((maxes[0] - mins[0]) / 2) - 1,
            //    (node.Y - mins[1]) / ((maxes[1] - mins[1]) / 2) - 1,
            //    (node.Z - mins[2]) / ((maxes[2] - mins[2]) / 2) - 1 };

            bool maybeInsideElement = node.X <= maxes[0] && node.X >= mins[0] &&
                                          node.Y <= maxes[1] && node.Y >= mins[1];
			if (maybeInsideElement == false) return new double[0];
           

            const int jacobianSize = 3;
			const int maxIterations = 1000;
			const double tolerance = 1e-10;
			int iterations = 0;
			double deltaNaturalCoordinatesNormSquare = 100;
			double[] naturalCoordinates = new double[] { 0, 0, 0 };
			const double toleranceSquare = tolerance * tolerance;

			while (deltaNaturalCoordinatesNormSquare > toleranceSquare && iterations < maxIterations)
			{
				iterations++;
				var shapeFunctions = CalcQ4Shape(naturalCoordinates[0], naturalCoordinates[1]);
				double[] coordinateDifferences = new double[] { 0, 0 };
				for (int i = 0; i < shapeFunctions.Length; i++)
				{
					coordinateDifferences[0] += shapeFunctions[i] * element.Nodes[i].X;
					coordinateDifferences[1] += shapeFunctions[i] * element.Nodes[i].Y;
				}
				coordinateDifferences[0] = node.X - coordinateDifferences[0];
				coordinateDifferences[1] = node.Y - coordinateDifferences[1];

				double[,] faXY = GetCoordinatesTranspose(element);
				double[] ShapeFunctionDerivatives = CalcQ4ShapeFunctionDerivatives(naturalCoordinates[0], naturalCoordinates[1]);
                //SOS PANOS - THIS IS WRONG, I SHOULD IMPLEMENT INVERSE JACOBIAN
                var inverseJacobian = CalcQ4J(faXY, ShapeFunctionDerivatives,naturalCoordinates[0], naturalCoordinates[1]);
                //SOS PANOS END
				double[] deltaNaturalCoordinates = new double[] { 0, 0, 0 };
				for (int i = 0; i < jacobianSize; i++)
					for (int j = 0; j < jacobianSize; j++)
						deltaNaturalCoordinates[i] += inverseJacobian[j, i] * coordinateDifferences[j];
				for (int i = 0; i < 3; i++)
					naturalCoordinates[i] += deltaNaturalCoordinates[i];

				deltaNaturalCoordinatesNormSquare = 0;
				for (int i = 0; i < 3; i++)
					deltaNaturalCoordinatesNormSquare += deltaNaturalCoordinates[i] * deltaNaturalCoordinates[i];
				//deltaNaturalCoordinatesNormSquare = Math.Sqrt(deltaNaturalCoordinatesNormSquare);
			}

			return naturalCoordinates.Count(x => Math.Abs(x) - 1.0 > tolerance) > 0 ? new double[0] : naturalCoordinates;
		}

		#endregion
	}
}

