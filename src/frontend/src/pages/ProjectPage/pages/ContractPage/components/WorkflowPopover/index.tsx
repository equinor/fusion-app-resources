import * as React from 'react';
import * as styles from './styles.less';
import classNames from 'classnames';
import { WorkflowStep } from '../../../../../../models/Workflow';
import { PersonCard } from '@equinor/fusion-components';
import * as moment from 'moment';

type WorkflowPopoverProps = {
    step: WorkflowStep;
};

const WorkflowPopover: React.FC<WorkflowPopoverProps> = ({ step }) => {
    const createItemField = React.useCallback(
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

    return (
        <div className={styles.popoverContainer}>
            <span className={styles.header}>
                {step.name} - {step.state}
            </span>
            {createItemField('started', 'Started', () =>
                step.started ? moment(step.started).fromNow() : 'N/A'
            )}

            {createItemField('completed', 'Completed', () =>
                step.completed ? moment(step.completed).fromNow() : 'N/A'
            )}
            {createItemField('completedBy', 'Completed by', () =>
                step.completedBy?.azureUniquePersonId ? (
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
        </div>
    );
};
export default WorkflowPopover;
