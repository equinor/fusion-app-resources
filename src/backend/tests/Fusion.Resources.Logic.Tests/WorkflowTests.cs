using FluentAssertions;
using Fusion.Resources.Logic.Workflows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{
    public class WorkflowTests
    {
        static WorkflowTests()
        {
            WorkflowTypes = Assembly.GetAssembly(typeof(WorkflowDefinition))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(WorkflowDefinition)))
                .Select(t => new[] { t })
                .ToList();
        }

        private readonly IEnumerable<Type> workflowTypes;

        public static IEnumerable<object[]> WorkflowTypes { get; }

        public WorkflowTests()
        {
            workflowTypes = Assembly.GetAssembly(typeof(WorkflowDefinition))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(WorkflowDefinition)))
                .ToList();
        }

        [Theory]
        [MemberData(nameof(WorkflowTypes))]
        public void SupportsDefaultContstructor(Type wfType)
        {
            var type = Activator.CreateInstance(wfType);
        }

        [Theory]
        [MemberData(nameof(WorkflowTypes))]
        public void HaveStartStep(Type wfType)
        {
            var wf = Activator.CreateInstance(wfType) as WorkflowDefinition;

            var entryStep = wf.Steps.FirstOrDefault(s => s.PreviousStepId is null);
            entryStep.Should().NotBeNull();
        }

        [Theory]
        [MemberData(nameof(WorkflowTypes))]
        public void HaveExitStep(Type wfType)
        {
            var wf = Activator.CreateInstance(wfType) as WorkflowDefinition;

            var exitStep = wf.Steps.FirstOrDefault(s => s.NextStepId is null);
            exitStep.Should().NotBeNull();
        }

        [Theory]
        [MemberData(nameof(WorkflowTypes))]
        public void HaveNoSelfReference(Type wfType)
        {
            var wf = Activator.CreateInstance(wfType) as WorkflowDefinition;

            var hasSelfReference = wf.Steps.Any(s => s.PreviousStepId == s.Id || s.NextStepId == s.Id);
            hasSelfReference.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(WorkflowTypes))]
        public void HavePathFromStartToEnd(Type wfType)
        {
            const int maxStepCounter = 20;

            var wf = Activator.CreateInstance(wfType) as WorkflowDefinition;

            var entryStep = wf.Steps.FirstOrDefault(s => s.PreviousStepId is null);
            entryStep.Should().NotBeNull();

            var exitStep = wf.Steps.FirstOrDefault(s => s.NextStepId is null);
            exitStep.Should().NotBeNull();


            var flow = wf.Step(entryStep.Id);
            WorkflowStep currentStep;

            var idx = 0;
            do
            {
                currentStep = flow.NextStep();
                if (idx++ > maxStepCounter)
                    throw new InvalidOperationException("Reached max workflow step counter, might be a circular reference...");
            }
            while (currentStep.NextStepId != null);

            currentStep.Id.Should().Be(exitStep.Id);
        }

        [Theory]
        [MemberData(nameof(WorkflowTypes))]
        public void HavePathEndToStart(Type wfType)
        {
            const int maxStepCounter = 20;

            var wf = Activator.CreateInstance(wfType) as WorkflowDefinition;

            var entryStep = wf.Steps.FirstOrDefault(s => s.PreviousStepId is null);
            entryStep.Should().NotBeNull();

            var exitStep = wf.Steps.FirstOrDefault(s => s.NextStepId is null);
            exitStep.Should().NotBeNull();


            WorkflowStep currentStep = exitStep;

            var idx = 0;
            do
            {
                currentStep = wf[currentStep.PreviousStepId];
                
                if (idx++ > maxStepCounter)
                    throw new InvalidOperationException("Reached max workflow step counter, might be a circular reference...");
            }
            while (currentStep.PreviousStepId != null);

            currentStep.Id.Should().Be(entryStep.Id);
        }
    }
}
