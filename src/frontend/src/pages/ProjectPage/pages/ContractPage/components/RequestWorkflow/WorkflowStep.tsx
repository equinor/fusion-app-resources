import * as React from 'react';
import * as styles from './styles.less';
import { WorkflowStep } from '../../../../../../models/Workflow';
import {
    CheckCircleIcon,
    styling,
    CloseCircleIcon,
    PersonPhoto,
    usePopoverRef,
} from '@equinor/fusion-components';
import classNames from 'classnames';
import { formatDate } from '@equinor/fusion';
import WorkflowPopover from '../WorkflowPopover';

type WorkflowStepProps = {
    step: WorkflowStep;
    inline: boolean;
};

const WorkflowStep: React.FC<WorkflowStepProps> = ({ step, inline }) => {
    const [popoverRef] = usePopoverRef<HTMLDivElement>(
        <WorkflowPopover step={step} />,
        {
            justify: 'start',
            placement: 'below',
        },
        true,
        300
    );

    const stepTitle = React.useMemo(() => {
        switch (step.id) {
            case 'created':
                return 'Create';
            case 'contractorApproval':
                return 'Approve SR';
            case 'companyApproval':
                return 'Approve Equinor';
            case 'provisioning':
                return 'Provisioned';
            default:
                return '';
        }
    }, [step]);

    const icon = React.useMemo(() => {
        switch (step.state) {
            case 'Approved':
                return <CheckCircleIcon color={styling.colors.green} />;
            case 'Pending':
                return <CheckCircleIcon color={styling.colors.orange} />; //TODO:Implement correct icon
            case 'Rejected':
                return <CloseCircleIcon color={styling.colors.secondary} />;
            case 'Skipped':
                return <CheckCircleIcon color={styling.colors.blackAlt5} />;
            default:
                null;
        }
    }, [step]);

    const completedBy = React.useMemo(() => {
        const person = step.completedBy;
        if (!person || inline) {
            return null;
        }
        return (
            <>
                <div className={styles.stepPerson}>
                    <PersonPhoto personId={person.azureUniquePersonId} />

                    <div className={styles.stepPersonDetails}>
                        <span>{person.name}</span>
                        <a href={`mailto:${person.mail}`}>{person.mail}</a>
                        <span className={styles.completed}>{step.state}</span>
                        <span className={styles.completedDate}>
                            {step.completed ? formatDate(step.completed) : 'N/A'}
                        </span>
                    </div>
                </div>
            </>
        );
    }, [step, inline]);

    const workflowStepClasses = classNames(styles.workflowStep, {
        [styles.inline]: inline,
    });
    const connectorClasses = classNames(styles.stepConnectorLine, {
        [styles.approved]: step.state === 'Approved',
    });

    return (
        <div className={workflowStepClasses}>
            <div className={styles.stepHeader} ref={popoverRef}>
                <div>{icon}</div>
                {!inline && <span className={styles.stepTitle}>{stepTitle}</span>}
                {step.nextStep !== null && (
                    <div className={styles.stepConnector}>
                        <div className={connectorClasses}></div>
                    </div>
                )}
            </div>
            {!inline && completedBy}
        </div>
    );
};

export default WorkflowStep;
