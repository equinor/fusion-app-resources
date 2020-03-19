import * as React from "react";
import * as styles from "./styles.less";
import { WorkflowStep } from '../../../../../../models/Workflow';

type WorkflowStepProps = {
    step: WorkflowStep;
}

const WorkflowStep: React.FC<WorkflowStepProps> = ({step}) => {

    return (
        <div className={styles.workflowStep}>
            <div className={styles.stepHeader}>
                <span>{step.name}</span>
            </div>
        </div>
    )
};

export default WorkflowStep;