using System;
using System.Collections.Generic;
using ISAAR.MSolve.PreProcessor;
using ISAAR.MSolve.Solvers.Skyline;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.PreProcessor.Materials;
using ISAAR.MSolve.PreProcessor.Elements;
using ISAAR.MSolve.Matrices;

namespace ISAAR.MSolve.Problems.Quad4Example
{
	class Quad4Example
	{
		private static IList<Node> CreateNodes()
		{
			IList<Node> nodes = new List<Node>();
			Node node1 = new Node { ID = 1, X = 0.0, Y = 0.0, Z = 0.0 };
			Node node2 = new Node { ID = 2, X = 1.0, Y = 0.0, Z = 0.0 };
			Node node3 = new Node { ID = 3, X = 1.0, Y = 1.0, Z = 0.0 };
			Node node4 = new Node { ID = 4, X = 0.0, Y = 1.0, Z = 0.0 };

			nodes.Add(node1);
			nodes.Add(node2);
			nodes.Add(node3);
			nodes.Add(node4);

			return nodes;
		}

		static void Main(string[] args)
		{
			VectorExtensions.AssignTotalAffinityCount();
			double youngModulus = 3.0e07;
			double poissonRatio = 0.3;
			double nodalLoad = 1000;

			// Create a new elastic 2D material
			ElasticMaterial2D material = new ElasticMaterial2D()
			{
				YoungModulus = youngModulus,
				PoissonRatio = poissonRatio
			};

			// Node creation
			IList<Node> nodes = CreateNodes();

			// Model creation
			Model model = new Model();

			// Add a single subdomain to the model
			model.SubdomainsDictionary.Add(1, new Subdomain() { ID = 1 });

			// Add nodes to the nodes dictonary of the model
			for (int i = 0; i < nodes.Count; ++i)
			{
				model.NodesDictionary.Add(i + 1, nodes[i]);
			}

			// Constrain left nodes of the model
			model.NodesDictionary[1].Constraints.Add(DOFType.X);
			model.NodesDictionary[1].Constraints.Add(DOFType.Y); 
			model.NodesDictionary[1].Constraints.Add(DOFType.Z); //Panos - I leave Z here
			model.NodesDictionary[4].Constraints.Add(DOFType.X);
			model.NodesDictionary[4].Constraints.Add(DOFType.Y);
			model.NodesDictionary[4].Constraints.Add(DOFType.Z); //Panos - Also here


			// Create a new Quad4 element
			var element = new Element()
			{
				ID = 1,
				ElementType = new Quad4(material)
			};

			// Add nodes to the created element
			element.AddNode(model.NodesDictionary[1]);
			element.AddNode(model.NodesDictionary[2]);
			element.AddNode(model.NodesDictionary[3]);
			element.AddNode(model.NodesDictionary[4]);


			// Add Quad4 element to the element and subdomains dictionary of the model
			model.ElementsDictionary.Add(element.ID, element);
			model.SubdomainsDictionary[1].ElementsDictionary.Add(element.ID, element);

			// Add nodal load values to node 3
			model.Loads.Add(new Load() { Amount = nodalLoad, Node = model.NodesDictionary[3], DOF = DOFType.X });

			// Needed in order to make all the required data structures
			model.ConnectDataStructures();

			// Choose linear equation system solver
			SolverSkyline solver = new SolverSkyline(model);

			// Choose the provider of the problem -> here a structural problem
			ProblemStructural provider = new ProblemStructural(model, solver.SubdomainsDictionary);

			// Choose parent and child analyzers -> Parent: Static, Child: Linear
			Analyzers.LinearAnalyzer childAnalyzer = new LinearAnalyzer(solver, solver.SubdomainsDictionary);
			StaticAnalyzer parentAnalyzer = new StaticAnalyzer(provider, childAnalyzer, solver.SubdomainsDictionary);

			// Choose dof types X, Y, Z to log for node 2
			 childAnalyzer.LogFactories[1] = new LinearAnalyzerLogFactory(new int[] {
				model.NodalDOFsDictionary[2][DOFType.X],
				model.NodalDOFsDictionary[2][DOFType.Y],
              // Choose dof types X, Y, Z to log for node 3
                model.NodalDOFsDictionary[3][DOFType.X],
                model.NodalDOFsDictionary[3][DOFType.Y]});

			// Analyze the problem
			parentAnalyzer.BuildMatrices();
			parentAnalyzer.Initialize();
			parentAnalyzer.Solve();

			// Write results to console
			Console.WriteLine("Writing results for node 2 and node 3");
			Console.WriteLine("Dof and Values for Displacement X, Y");
            Console.WriteLine(childAnalyzer.Logs[1][0]);


		}
	}
}