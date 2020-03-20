import * as React from 'react';
import * as styles from './styles.less';
import Workflow from '../../../../../../models/Workflow';
import WorkflowStep from './WorkflowStep';

type RequestWorkflowProps = {
    workflow: Workflow;
    inline?: boolean
};

const RequestWorkflow: React.FC<RequestWorkflowProps> = ({ workflow, inline }) => {
    const sortedWorkflowSteps = React.useMemo(() => {
        const sortByObject = {
            created: 0,
            contractorApproval: 1,
            companyApproval: 2,
            provisioning: 3,
        };

        return workflow.steps.sort((a, b) => sortByObject[a['id']] - sortByObject[b['id']]);
    }, [workflow]);

    return (
        <div className={styles.workflowContainer}>
            {sortedWorkflowSteps.map(step => (
                <WorkflowStep step={step} inline={!!inline} />
            ))}
        </div>
    );
};

export default RequestWorkflow;
