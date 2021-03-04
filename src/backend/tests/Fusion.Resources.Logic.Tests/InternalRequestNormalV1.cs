using FluentAssertions;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Workflows;
using System;
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
            var workflow = new InternalRequestNormalWorkflowV1();

            workflow.Steps.Should().HaveCount(4);
            workflow.Steps.Should().Contain(s => s.Id == InternalRequestNormalWorkflowV1.CREATED);
            workflow.Steps.Should().Contain(s => s.Id == InternalRequestNormalWorkflowV1.PROPOSAL);
            workflow.Steps.Should().Contain(s => s.Id == InternalRequestNormalWorkflowV1.APPROVAL);
            workflow.Steps.Should().Contain(s => s.Id == InternalRequestNormalWorkflowV1.PROVISIONING);
        }

        [Theory]
        [InlineData(InternalRequestNormalWorkflowV1.CREATED, InternalRequestNormalWorkflowV1.PROPOSAL)]
        [InlineData(InternalRequestNormalWorkflowV1.PROPOSAL, InternalRequestNormalWorkflowV1.APPROVAL)]
        [InlineData(InternalRequestNormalWorkflowV1.APPROVAL, InternalRequestNormalWorkflowV1.PROVISIONING)]
        [InlineData(InternalRequestNormalWorkflowV1.PROVISIONING, null)]
        
        public void Steps_Should_HaveNextStep(string stepId, string nextStep)
        {
            var workflow = new InternalRequestNormalWorkflowV1();
            workflow[stepId].NextStepId.Should().Be(nextStep);
        }

        [Theory]
        [InlineData(InternalRequestNormalWorkflowV1.CREATED, null)]
        [InlineData(InternalRequestNormalWorkflowV1.PROPOSAL, InternalRequestNormalWorkflowV1.CREATED)]
        [InlineData(InternalRequestNormalWorkflowV1.APPROVAL, InternalRequestNormalWorkflowV1.PROPOSAL)]
        [InlineData(InternalRequestNormalWorkflowV1.PROVISIONING, InternalRequestNormalWorkflowV1.APPROVAL)]
        public void Steps_Should_HavePreviousStep(string stepId, string prevStep)
        {
            var workflow = new InternalRequestNormalWorkflowV1();
            workflow[stepId].PreviousStepId.Should().Be(prevStep);
        }

        [Fact]
        public void NewRequest_Should_PopulateCreatedStep()
        {
            var workflow = new InternalRequestNormalWorkflowV1(creator);

            workflow[InternalRequestNormalWorkflowV1.CREATED].CompletedBy.Should().Be(creator);
            workflow[InternalRequestNormalWorkflowV1.CREATED].Completed.Should().NotBeNull();
            workflow[InternalRequestNormalWorkflowV1.PROPOSAL].State.Should().Be(DbWFStepState.Pending);
            workflow[InternalRequestNormalWorkflowV1.PROPOSAL].Started.Should().NotBeNull();
        }

        [Fact]
        public void CreatedRequest_Should_UpdateCorrectly_WhenApproved()
        {
            var workflow = new InternalRequestNormalWorkflowV1(creator);

            workflow.Proposed(resourceOwner);

            workflow[InternalRequestNormalWorkflowV1.PROPOSAL].CompletedBy.Should().Be(resourceOwner);
            workflow[InternalRequestNormalWorkflowV1.PROPOSAL].State.Should().Be(DbWFStepState.Approved);
            workflow[InternalRequestNormalWorkflowV1.PROPOSAL].Completed.Should().NotBeNull();
            workflow[InternalRequestNormalWorkflowV1.APPROVAL].State.Should().Be(DbWFStepState.Pending);
            workflow[InternalRequestNormalWorkflowV1.APPROVAL].Started.Should().NotBeNull();
        }

        [Fact]
        public void ProposedRequest_Should_UpdateCorrectly_WhenApproved()
        {
            var workflow = new InternalRequestNormalWorkflowV1(creator);
            workflow.Proposed(resourceOwner);
            workflow.Approved(taskOwner);

            workflow[InternalRequestNormalWorkflowV1.APPROVAL].CompletedBy.Should().Be(taskOwner);
            workflow[InternalRequestNormalWorkflowV1.APPROVAL].State.Should().Be(DbWFStepState.Approved);
            workflow[InternalRequestNormalWorkflowV1.APPROVAL].Completed.Should().NotBeNull();
            workflow[InternalRequestNormalWorkflowV1.PROVISIONING].State.Should().Be(DbWFStepState.Pending);
            workflow[InternalRequestNormalWorkflowV1.PROVISIONING].Started.Should().NotBeNull();
        }
    }
}
