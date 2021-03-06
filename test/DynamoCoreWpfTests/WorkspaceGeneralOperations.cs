﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using NUnit.Framework;
using Dynamo.Selection;
using System.IO;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Nodes.ZeroTouch;
using Dynamo.Graph.Workspaces;
using Dynamo.Models;
using Dynamo.Views;

namespace Dynamo.Tests
{
    public class WorkspaceGeneralOperations : DynamoViewModelUnitTest
    {
        protected override void GetLibrariesToPreload(List<string> libraries)
        {
            libraries.Add("VMDataBridge.dll");  // Required for reading preview bubble data
            base.GetLibrariesToPreload(libraries);
        }

        [Test]
        public void SwitchingToCustomWorkspaceWithSelectionShouldNotAllowGeometricOperations()
        {
            var model = ViewModel.Model; // The current DynamoModel instance.

            // Step 0: Create a new node in Home workspace.
            var addNode = new DSFunction(model.LibraryServices.GetFunctionDescriptor("+"));
            model.CurrentWorkspace.AddAndRegisterNode(addNode, false);
            Assert.AreEqual(model.CurrentWorkspace.Nodes.Count(), 1);

            // Step 1: Select the newly node, geometry operation should be enabled.
            DynamoSelection.Instance.Selection.Add(addNode);
            Assert.AreEqual(true, ViewModel.CurrentSpaceViewModel.HasSelection);
            Assert.AreEqual(true, ViewModel.CurrentSpaceViewModel.IsGeometryOperationEnabled);

            // Step 2: Open a Custom workspace.
            var customNodePath = Path.Combine(TestDirectory, @"core\CustomNodes\NoInput.dyf");
            ViewModel.OpenCommand.Execute(customNodePath);
            var customWorkspace = model.Workspaces.FirstOrDefault(x => x is CustomNodeWorkspaceModel);
            Assert.IsNotNull(customWorkspace);

            // Step 3: Switch over from home workspace to custom workspace.
            Assert.IsTrue(ViewModel.CurrentSpaceViewModel.Model is HomeWorkspaceModel);
            ViewModel.CurrentWorkspaceIndex = 1;
            Assert.IsTrue(ViewModel.CurrentSpaceViewModel.Model is CustomNodeWorkspaceModel);

            // Step 4: Verify that the geometry operations are 
            // disabled despite the fact that there is still selection.
            Assert.AreEqual(true, ViewModel.CurrentSpaceViewModel.HasSelection);
            Assert.AreEqual(false, ViewModel.CurrentSpaceViewModel.IsGeometryOperationEnabled);
        }

        [Test]
        public void VerifyGlobalLacingStrategyUpdatesForLacingStrategies()
        {
            var openPath = Path.Combine(TestDirectory, @"core\visualization\LacingStrategyGlobal.dyn");
            ViewModel.OpenCommand.Execute(openPath);

            var workspace = ViewModel.Model.CurrentWorkspace as HomeWorkspaceModel;
            workspace.RunSettings.RunType = RunType.Automatic;

            // Identifiers for all nodes in the workspace.
            var nodeIds = new[]
            {
                Guid.Parse("0750d269-26f3-47d7-bb25-79c762a540ff"), // point0
                Guid.Parse("33b03fa4-50e7-4d08-a5d8-ae7f168c2624"), // point1
                Guid.Parse("fa226a63-1a31-4f13-8b93-1286dac2c695"), // point2
                Guid.Parse("e7b0f340-a0bc-46b2-a05d-7dd71c72e27c"), // arrays codeblock [1..5] and [11..12]
            };

            // Verify node state in workspace
            var nodes = nodeIds.Select(workspace.NodeFromWorkspace).ToList();
            Assert.IsFalse(nodes.Any(n => n == null)); // Nothing should be null.

            // Verify initial lacing state is Auto and nodes return correct number of points
            // 5 points for point0 node, 2 points for point1 and point2 nodes
            AssertPreviewCount(nodeIds[0].ToString(), 5);
            AssertPreviewCount(nodeIds[1].ToString(), 2);
            AssertPreviewCount(nodeIds[2].ToString(), 2);

            // Modify lacing strategy to Longest
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.Longest.ToString());

            AssertPreviewCount(nodeIds[0].ToString(), 5);
            AssertPreviewCount(nodeIds[1].ToString(), 2);
            AssertPreviewCount(nodeIds[2].ToString(), 5);

            // Modify lacing strategy to Auto
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.Auto.ToString());

            AssertPreviewCount(nodeIds[0].ToString(), 5);
            AssertPreviewCount(nodeIds[1].ToString(), 2);
            AssertPreviewCount(nodeIds[2].ToString(), 2);

            // Modify lacing strategy to CrossProduct
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.CrossProduct.ToString());

            AssertPreviewCount(nodeIds[0].ToString(), 5);
            AssertPreviewCount(nodeIds[1].ToString(), 2);
            AssertPreviewCount(nodeIds[2].ToString(), 5);

            // Change lacing back to Shortest
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.Shortest.ToString());

            AssertPreviewCount(nodeIds[0].ToString(), 5);
            AssertPreviewCount(nodeIds[1].ToString(), 2);
            AssertPreviewCount(nodeIds[2].ToString(), 2);
        }
        
        [Test]
        public void VerifyLacingStrategyDisabledCantBeChanged()
        {
            // Arrange
            var openPath = Path.Combine(TestDirectory, @"core\visualization\LacingStrategyDisabled.dyn");
            ViewModel.OpenCommand.Execute(openPath);
            var workspace = ViewModel.Model.CurrentWorkspace as HomeWorkspaceModel;
            Guid codeBlockNodeId = Guid.Parse("5a35517215434699afe122bc51aeff7d");
            Guid pythonNodeId = Guid.Parse("cd679ac2203f42c18b30e44bb6c5238e");
            Guid typicalNodeId = Guid.Parse("ab8afb7c1dfe4dd0994662f3306fc530");
            var codeBlockNode = workspace.NodeFromWorkspace(codeBlockNodeId);
            var pythonNode = workspace.NodeFromWorkspace(pythonNodeId);
            var typicalNode = workspace.NodeFromWorkspace(typicalNodeId);

            // Verify initial lacing state is Auto
            var codeBlockNodeLacingStart = codeBlockNode.ArgumentLacing;
            var pythonNodeLacingStart = pythonNode.ArgumentLacing;
            var typicalNodeLacingStart = typicalNode.ArgumentLacing;

            // Act

            // Modify lacing strategy to Longest
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.Longest.ToString());
            var codeBlockNodeLacingLongest = codeBlockNode.ArgumentLacing;
            var pythonNodeLacingLongest = pythonNode.ArgumentLacing;
            var typicalNodeLacingLongest = typicalNode.ArgumentLacing;

            // Modify lacing strategy to Auto
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.Auto.ToString());
            var codeBlockNodeLacingAuto = codeBlockNode.ArgumentLacing;
            var pythonNodeLacingAuto = pythonNode.ArgumentLacing;
            var typicalNodeLacingAuto = typicalNode.ArgumentLacing;

            // Modify lacing strategy to CrossProduct
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.CrossProduct.ToString());
            var codeBlockNodeLacingCross = codeBlockNode.ArgumentLacing;
            var pythonNodeLacingCross = pythonNode.ArgumentLacing;
            var typicalNodeLacingCross = typicalNode.ArgumentLacing;

            // Change lacing back to Shortest
            ViewModel.CurrentSpaceViewModel.SelectAllCommand.Execute(null);
            ViewModel.CurrentSpaceViewModel.SetArgumentLacingCommand.Execute(LacingStrategy.Shortest.ToString());
            var codeBlockNodeLacingShortest = codeBlockNode.ArgumentLacing;
            var pythonNodeLacingShortest = pythonNode.ArgumentLacing;
            var typicalNodeLacingShortest = typicalNode.ArgumentLacing;

            // Assert
            Assert.AreEqual(LacingStrategy.Disabled, codeBlockNodeLacingStart);
            Assert.AreEqual(LacingStrategy.Disabled, pythonNodeLacingStart);
            Assert.AreEqual(LacingStrategy.Auto, typicalNodeLacingStart);
            Assert.AreEqual(LacingStrategy.Disabled, codeBlockNodeLacingLongest);
            Assert.AreEqual(LacingStrategy.Disabled, pythonNodeLacingLongest);
            Assert.AreEqual(LacingStrategy.Longest, typicalNodeLacingLongest);
            Assert.AreEqual(LacingStrategy.Disabled, codeBlockNodeLacingAuto);
            Assert.AreEqual(LacingStrategy.Disabled, pythonNodeLacingAuto);
            Assert.AreEqual(LacingStrategy.Auto, typicalNodeLacingAuto);
            Assert.AreEqual(LacingStrategy.Disabled, codeBlockNodeLacingCross);
            Assert.AreEqual(LacingStrategy.Disabled, pythonNodeLacingCross);
            Assert.AreEqual(LacingStrategy.CrossProduct, typicalNodeLacingCross);
            Assert.AreEqual(LacingStrategy.Disabled, codeBlockNodeLacingShortest);
            Assert.AreEqual(LacingStrategy.Disabled, pythonNodeLacingShortest);
            Assert.AreEqual(LacingStrategy.Shortest, typicalNodeLacingShortest);
        }
        
        [Test]
        public void AreGlobalLacingStrategiesInMenu()
        {
            // Mock a WorkspaceView
            var workspaceView = new WorkspaceView();

            // Search the associated context menu for the lacing sub-menu
            var contextMenu = workspaceView.FindName("WorkspaceLacingMenu") as MenuItem;

            // Auto, Shortest, Longest, Cross Product
            Assert.AreEqual(contextMenu.Items.Count, 4);
        }
    }
}
