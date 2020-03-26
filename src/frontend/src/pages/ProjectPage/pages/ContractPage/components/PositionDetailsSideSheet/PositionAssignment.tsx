import * as React from 'react';
import { PersonCard, PersonPhoto } from '@equinor/fusion-components';
import { PositionInstance, formatDate } from '@equinor/fusion';

import * as styles from './styles.less';

type PositionAssignmentProps = {
    instance?: PositionInstance;
};
const PositionAssignment: React.FC<PositionAssignmentProps> = ({ instance }) => {
    if (!instance) {
        return null;
    }
    return (
        <div className={styles.instance}>
            <div className={styles.rotationIdContainer}>
                {instance.rotationId ? (
                    <div className={styles.rotationId}>R{instance.rotationId.toUpperCase()}</div>
                ) : null}
            </div>
            <div className={styles.personCard}>
                {instance.assignedPerson ? (
                    <PersonCard person={instance.assignedPerson || undefined} />
                ) : (
                    <div className={styles.noPerson}>
                        <PersonPhoto size="xlarge" /> <div className={styles.tbnName}>TBN</div>
                    </div>
                )}
            </div>
            <div className={styles.segment}>
                <h4>From</h4>
                <p>{formatDate(instance.appliesFrom)}</p>
            </div>
            <div className={styles.segment}>
                <h4>To</h4>
                <p>{formatDate(instance.appliesTo)}</p>
            </div>
            <div className={styles.segment}>
                <h4>%</h4>
                <p>{instance.workload}</p>
            </div>
        </div>
    );
};

export default PositionAssignment;
