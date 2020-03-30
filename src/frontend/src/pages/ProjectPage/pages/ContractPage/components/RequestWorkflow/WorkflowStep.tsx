import * as React from 'react';
import * as styles from './styles.less';
import { WorkflowStep } from '../../../../../../models/Workflow';
import {
    CheckCircleIcon,
    styling,
    CloseCircleIcon,
    PersonPhoto,
    usePopoverRef,
    ScheduleIcon,
} from '@equinor/fusion-components';
import classNames from 'classnames';
import { formatDate } from '@equinor/fusion';
import WorkflowPopover from '../WorkflowPopover';
import ProvisioningStatus from '../../../../../../models/ProvisioningStatus ';
import FusionIcon from '../FusionIcon';

type RequestWorkflowStepProps = {
    step: WorkflowStep;
    provisioningStatus: ProvisioningStatus;
    inline: boolean;
};

const RequestWorkflowStep: React.FC<RequestWorkflowStepProps> = ({
    step,
    inline,
    provisioningStatus,
}) => {
    const [popoverRef] = usePopoverRef<HTMLDivElement>(
        <WorkflowPopover step={step} provisioningStatus={provisioningStatus} />,
        {
            justify: 'center',
            placement: 'below',
        },
        true,
        300
    );

    const hasProvisioned = React.useMemo(
        () => step.id === 'provisioning' && provisioningStatus.state === 'Provisioned',
        [provisioningStatus, step]
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
        if (hasProvisioned) {
            return <CheckCircleIcon color={styling.colors.green} />;
        }
        switch (step.state) {
            case 'Approved':
                return <CheckCircleIcon color={styling.colors.green} />;
            case 'Pending':
                return <ScheduleIcon color={styling.colors.orange} />;
            case 'Rejected':
                return <CloseCircleIcon color={styling.colors.red} />;
            case 'Skipped':
                return <CheckCircleIcon color={styling.colors.blackAlt3} />;
            default:
                null;
        }
    }, [step, hasProvisioned]);

    const completedBy = React.useMemo(() => {
        const person = step.completedBy;
        if (hasProvisioned) {
            return (
                <div className={styles.stepPerson}>
                    <FusionIcon  />
                    <div className={styles.stepPersonDetails}>
                        <span>System account</span>
                        <span className={styles.completed}>{provisioningStatus.state}</span>
                        <span className={styles.completedDate}>
                            {provisioningStatus.provisioned
                                ? formatDate(provisioningStatus.provisioned)
                                : 'N/A'}
                        </span>
                    </div>
                </div>
            );
        }
        if (!person || inline) {
            return null;
        }
        return (
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
        );
    }, [step, inline, provisioningStatus, hasProvisioned]);

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

export default RequestWorkflowStep;
