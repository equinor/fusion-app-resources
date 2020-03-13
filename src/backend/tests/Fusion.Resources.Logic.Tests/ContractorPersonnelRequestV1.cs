using FluentAssertions;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Workflows;
using System;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{
    public class ContractorPersonnelRequestV1
    {

        private DbPerson creator = new DbPerson { Id = Guid.NewGuid(), Name = "Creator" };
        private DbPerson contractorRep = new DbPerson { Id = Guid.NewGuid(), Name = "Contractor Rep" };
        private DbPerson companyRep = new DbPerson { Id = Guid.NewGuid(), Name = "Company Rep" };


        [Fact]
        public void Create_Should_InitializeWithAllSteps()
        {
            var workflow = new ContractorPersonnelWorkflowV1();

            workflow.Steps.Should().HaveCount(4);
            workflow.Steps.Should().Contain(s => s.Id == ContractorPersonnelWorkflowV1.CREATED);
            workflow.Steps.Should().Contain(s => s.Id == ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL);
            workflow.Steps.Should().Contain(s => s.Id == ContractorPersonnelWorkflowV1.COMPANY_APPROVAL);
            workflow.Steps.Should().Contain(s => s.Id == ContractorPersonnelWorkflowV1.PROVISIONING);
        }

        [Theory]
        [InlineData(ContractorPersonnelWorkflowV1.CREATED, ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL)]
        [InlineData(ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL, ContractorPersonnelWorkflowV1.COMPANY_APPROVAL)]
        [InlineData(ContractorPersonnelWorkflowV1.COMPANY_APPROVAL, ContractorPersonnelWorkflowV1.PROVISIONING)]
        [InlineData(ContractorPersonnelWorkflowV1.PROVISIONING, null)]
        public void Steps_Should_HaveNextStep(string stepId, string nextStep)
        {
            var workflow = new ContractorPersonnelWorkflowV1();
            workflow[stepId].NextStepId.Should().Be(nextStep);
        }

        [Theory]
        [InlineData(ContractorPersonnelWorkflowV1.CREATED, null)]
        [InlineData(ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL, ContractorPersonnelWorkflowV1.CREATED)]
        [InlineData(ContractorPersonnelWorkflowV1.COMPANY_APPROVAL, ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL)]
        [InlineData(ContractorPersonnelWorkflowV1.PROVISIONING, ContractorPersonnelWorkflowV1.COMPANY_APPROVAL)]
        public void Steps_Should_HavePreviousStep(string stepId, string prevStep)
        {
            var workflow = new ContractorPersonnelWorkflowV1();
            workflow[stepId].PreviousStepId.Should().Be(prevStep);
        }

        [Fact]
        public void NewRequest_Should_PopulateCreatedStep()
        {
            var workflow = new ContractorPersonnelWorkflowV1(creator);

            workflow[ContractorPersonnelWorkflowV1.CREATED].CompletedBy.Should().Be(creator);
            workflow[ContractorPersonnelWorkflowV1.CREATED].Completed.Should().NotBeNull();
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].State.Should().Be(DbWFStepState.Pending);
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].Started.Should().NotBeNull();
        }

        [Fact]
        public void CreatedRequest_Should_UpdateCorrectly_WhenApproved()
        {
            var workflow = new ContractorPersonnelWorkflowV1(creator);

            workflow.ContractorApproved(contractorRep);

            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].CompletedBy.Should().Be(contractorRep);
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].State.Should().Be(DbWFStepState.Approved);
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].Completed.Should().NotBeNull();
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].State.Should().Be(DbWFStepState.Pending);
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].Started.Should().NotBeNull();
        }

        [Fact]
        public void CreatedRequest_Should_UpdateCorrectly_WhenRejected()
        {
            var reason = "Reject reason";
            
            var workflow = new ContractorPersonnelWorkflowV1(creator);
            workflow.ContractorRejected(contractorRep, reason);

            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].CompletedBy.Should().Be(contractorRep);
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].Completed.Should().NotBeNull();
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].Reason.Should().Be(reason);
            workflow[ContractorPersonnelWorkflowV1.CONTRACTOR_APPROVAL].State.Should().Be(DbWFStepState.Rejected);
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].State.Should().Be(DbWFStepState.Skipped);
            workflow[ContractorPersonnelWorkflowV1.PROVISIONING].State.Should().Be(DbWFStepState.Skipped);
        }

        [Fact]
        public void ContractorApprovedRequest_Should_UpdateCorrectly_WhenApproved()
        {
            var workflow = new ContractorPersonnelWorkflowV1(creator);
            workflow.ContractorApproved(contractorRep);
            workflow.CompanyApproved(companyRep);

            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].CompletedBy.Should().Be(companyRep);
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].State.Should().Be(DbWFStepState.Approved);
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].Completed.Should().NotBeNull();
            workflow[ContractorPersonnelWorkflowV1.PROVISIONING].State.Should().Be(DbWFStepState.Pending);
            workflow[ContractorPersonnelWorkflowV1.PROVISIONING].Started.Should().NotBeNull();
        }

        [Fact]
        public void ContractorApprovedRequest_Should_UpdateCorrectly_WhenRejected()
        {
            var reason = "Reject reason";

            var workflow = new ContractorPersonnelWorkflowV1(creator);
            workflow.ContractorApproved(contractorRep);
            workflow.CompanyRejected(companyRep, reason);

            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].CompletedBy.Should().Be(companyRep);
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].Completed.Should().NotBeNull();
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].Reason.Should().Be(reason);
            workflow[ContractorPersonnelWorkflowV1.COMPANY_APPROVAL].State.Should().Be(DbWFStepState.Rejected);
            workflow[ContractorPersonnelWorkflowV1.PROVISIONING].State.Should().Be(DbWFStepState.Skipped);
        }
    }
}
