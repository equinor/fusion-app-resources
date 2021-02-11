
import styles from './styles.less';
import classNames from 'classnames';
import { WorkflowStep } from '../../../../../../models/Workflow';
import { PersonCard } from '@equinor/fusion-components';
import * as moment from 'moment';
import ProvisioningStatus from '../../../../../../models/ProvisioningStatus ';
import FusionIcon from '../FusionIcon';
import { FC, useCallback, useMemo } from 'react';

type WorkflowPopoverProps = {
    step: WorkflowStep;
    provisioningStatus: ProvisioningStatus;
};

const WorkflowPopover: FC<WorkflowPopoverProps> = ({ step, provisioningStatus }) => {
    const createItemField = useCallback(
        (fieldName: string, title: string, content: () => string | JSX.Element) => {
            return (
                <div className={classNames(styles.textField, styles[fieldName])}>
                    <span className={styles.title}>{title}</span>
                    <span className={styles.content}>{content()}</span>
                </div>
            );
        },
        []
    );

    const hasProvisioned = useMemo(
        () => step.id === 'provisioning' && provisioningStatus.state === 'Provisioned',
        [provisioningStatus, step]
    );

    return (
        <div className={styles.popoverContainer}>
            <span className={styles.header}>
                {step.name} - {hasProvisioned ? provisioningStatus.state : step.state}
            </span>
            {createItemField('started', 'Started', () =>
                step.started ? moment(step.started).fromNow() : 'N/A'
            )}

            {createItemField('completed', 'Completed', () =>
                step.completed ? moment(step.completed).fromNow() : 'N/A'
            )}
            {createItemField('completedBy', 'Completed by', () =>
                hasProvisioned ? (
                    <div className={styles.stepPerson}>
                        <FusionIcon />
                        <div className={styles.stepPersonDetails}>
                            <span>System account</span>
                        </div>
                    </div>
                ) : step.completedBy?.azureUniquePersonId ? (
                    <PersonCard
                        personId={step.completedBy?.azureUniquePersonId}
                        photoSize="large"
                    />
                ) : (
                    'TBN'
                )
            )}
            {createItemField('description', 'Description', () =>
                step.description.length > 0 ? step.description : 'N/A'
            )}
            {step.isCompleted && step.state === 'Rejected' ? createItemField('reason', 'Reason', () =>
                step.reason || ""
            ) : null}
        </div>
    );
};
export default WorkflowPopover;
