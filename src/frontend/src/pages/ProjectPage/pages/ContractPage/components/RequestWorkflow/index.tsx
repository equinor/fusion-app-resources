import * as React from 'react';
import * as styles from './styles.less';
import Workflow from '../../../../../../models/Workflow';
import { WorkflowStep } from '../../../../../../models/Workflow';
import RequestWorkflowStep from './WorkflowStep';

type RequestWorkflowProps = {
    workflow: Workflow;
    inline?: boolean;
};

const RequestWorkflow: React.FC<RequestWorkflowProps> = ({ workflow, inline }) => {
    const findWorkflowIndex = React.useCallback(
        (step: WorkflowStep, index: number): number => {
            const previousStep = workflow.steps.find(
                workflowStep => workflowStep.id === step.previousStep
            );

            if (previousStep) {
                return findWorkflowIndex(previousStep, index + 1);
            }
            return index;
        },
        [workflow]
    );

    const sortedWorkflowSteps = React.useMemo(
        () =>
            workflow.steps.reduce((sortedSteps: WorkflowStep[], currentStep: WorkflowStep) => {
                sortedSteps[findWorkflowIndex(currentStep, 0)] = currentStep;
                return sortedSteps;
            }, new Array(workflow.steps.length).fill(null)),
        [workflow]
    );

    return (
        <div className={styles.workflowContainer}>
            {sortedWorkflowSteps.map(step => (
                <RequestWorkflowStep step={step} inline={!!inline} />
            ))}
        </div>
    );
};

export default RequestWorkflow;
