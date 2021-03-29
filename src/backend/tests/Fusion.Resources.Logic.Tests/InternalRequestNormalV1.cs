using FluentAssertions;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Workflows;
using System;
using System.Collections;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{

    public class InternalRequestNormalV1
    {

        private DbPerson creator = new DbPerson { Id = Guid.NewGuid(), Name = "Creator" };
        private DbPerson resourceOwner = new DbPerson { Id = Guid.NewGuid(), Name = "Resource owner" };
        private DbPerson taskOwner = new DbPerson { Id = Guid.NewGuid(), Name = "Task owner" };


        [Fact]
        public void Create_Should_InitializeWithAllSteps()
        {
            var workflow = new AllocationNormalWorkflowV1();

            workflow.Steps.Should().HaveCount(4);
            workflow.Steps.Should().Contain(s => s.Id == AllocationNormalWorkflowV1.CREATED);
            workflow.Steps.Should().Contain(s => s.Id == AllocationNormalWorkflowV1.PROPOSAL);
            workflow.Steps.Should().Contain(s => s.Id == AllocationNormalWorkflowV1.APPROVAL);
            workflow.Steps.Should().Contain(s => s.Id == AllocationNormalWorkflowV1.PROVISIONING);
        }

        [Theory]
        [InlineData(AllocationNormalWorkflowV1.CREATED, AllocationNormalWorkflowV1.PROPOSAL)]
        [InlineData(AllocationNormalWorkflowV1.PROPOSAL, AllocationNormalWorkflowV1.APPROVAL)]
        [InlineData(AllocationNormalWorkflowV1.APPROVAL, AllocationNormalWorkflowV1.PROVISIONING)]
        [InlineData(AllocationNormalWorkflowV1.PROVISIONING, null)]

        public void Steps_Should_HaveNextStep(string stepId, string nextStep)
        {
            var workflow = new AllocationNormalWorkflowV1();
            workflow[stepId].NextStepId.Should().Be(nextStep);
        }

        [Theory]
        [InlineData(AllocationNormalWorkflowV1.CREATED, null)]
        [InlineData(AllocationNormalWorkflowV1.PROPOSAL, AllocationNormalWorkflowV1.CREATED)]
        [InlineData(AllocationNormalWorkflowV1.APPROVAL, AllocationNormalWorkflowV1.PROPOSAL)]
        [InlineData(AllocationNormalWorkflowV1.PROVISIONING, AllocationNormalWorkflowV1.APPROVAL)]
        public void Steps_Should_HavePreviousStep(string stepId, string prevStep)
        {
            var workflow = new AllocationNormalWorkflowV1();
            workflow[stepId].PreviousStepId.Should().Be(prevStep);
        }

        [Fact]
        public void NewRequest_Should_PopulateCreatedStep()
        {
            var workflow = new AllocationNormalWorkflowV1(creator);

            workflow[AllocationNormalWorkflowV1.CREATED].CompletedBy.Should().Be(creator);
            workflow[AllocationNormalWorkflowV1.CREATED].Completed.Should().NotBeNull();
            workflow[AllocationNormalWorkflowV1.PROPOSAL].State.Should().Be(DbWFStepState.Pending);
            workflow[AllocationNormalWorkflowV1.PROPOSAL].Started.Should().NotBeNull();
        }

        [Fact]
        public void CreatedRequest_Should_UpdateCorrectly_WhenApproved()
        {
            var workflow = new AllocationNormalWorkflowV1(creator);

            workflow.Proposed(resourceOwner);

            workflow[AllocationNormalWorkflowV1.PROPOSAL].CompletedBy.Should().Be(resourceOwner);
            workflow[AllocationNormalWorkflowV1.PROPOSAL].State.Should().Be(DbWFStepState.Approved);
            workflow[AllocationNormalWorkflowV1.PROPOSAL].Completed.Should().NotBeNull();
            workflow[AllocationNormalWorkflowV1.APPROVAL].State.Should().Be(DbWFStepState.Pending);
            workflow[AllocationNormalWorkflowV1.APPROVAL].Started.Should().NotBeNull();
        }

        [Fact]
        public void ProposedRequest_Should_UpdateCorrectly_WhenApproved()
        {
            var workflow = new AllocationNormalWorkflowV1(creator);
            workflow.Proposed(resourceOwner);
            workflow.Approved(taskOwner);

            workflow[AllocationNormalWorkflowV1.APPROVAL].CompletedBy.Should().Be(taskOwner);
            workflow[AllocationNormalWorkflowV1.APPROVAL].State.Should().Be(DbWFStepState.Approved);
            workflow[AllocationNormalWorkflowV1.APPROVAL].Completed.Should().NotBeNull();
            workflow[AllocationNormalWorkflowV1.PROVISIONING].State.Should().Be(DbWFStepState.Pending);
            workflow[AllocationNormalWorkflowV1.PROVISIONING].Started.Should().NotBeNull();
        }
    }
}
