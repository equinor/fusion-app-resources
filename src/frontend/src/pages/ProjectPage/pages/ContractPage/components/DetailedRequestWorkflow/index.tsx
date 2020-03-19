import * as React from 'react';
import * as styles from './styles.less';
import Workflow from '../../../../../../models/Workflow';
import WorkflowStep from './WorkflowStep';

type DetailedRequestWorkflowProps = {
    workflow: Workflow;
};

const DetailedRequestWorkflow: React.FC<DetailedRequestWorkflowProps> = ({ workflow }) => {
    return (
        <div className={styles.workflowContainer}>
            {workflow.steps.map(step => (
                <WorkflowStep step={step} />
            ))}
        </div>
    );
};

export default DetailedRequestWorkflow;
