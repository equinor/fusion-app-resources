import * as React from 'react';
import Personnel, { Position } from '../../../../../../models/Personnel';
import * as styles from './styles.less';
import { PersonPosition } from '@equinor/fusion';
import { PersonPositionCard } from '@equinor/fusion-components';

type PersonPositionsDetailsProps = {
    person: Personnel;
};

const mapPositionsToPersonPosition = (p: Position): PersonPosition => {
    return {
        id: p.positionId,
        name: p.name,
        obs: p.obs,
        project: {
            id: p.project.projectId,
            name: p.project.name,
            domainId: p.project.domainId,
            type: p.project.projectType,
        },
        basePosition: p.basePosition,
        appliesFrom: p.appliesFrom,
        appliesTo: p.appliesTo,
        workload: p.workload,
    };
};

const PersonPositionsDetails: React.FC<PersonPositionsDetailsProps> = ({ person }) => {
    const positions = React.useMemo(() => person.positions?.map(mapPositionsToPersonPosition), [
        person,
    ]);

    const currentDate = React.useMemo(() => new Date().getTime(), []);
    const pastPositions = React.useMemo(
        () => positions?.filter(p => (p.appliesTo?.getTime() || currentDate) <= currentDate),
        [positions]
    );
    const activePositions = React.useMemo(
        () =>
            positions?.filter(
                p =>
                    (p.appliesFrom?.getTime() || currentDate) < currentDate &&
                    (p.appliesTo?.getTime() || currentDate) >= currentDate
            ),
        [positions]
    );

    return (
        <div className={styles.container}>
            <div className={styles.activePositions}>
                <h4>Active Positions</h4>
                {activePositions?.length ? (
                    <ul className={styles.positionList}>
                        {activePositions.map(position => (
                            <li key={position.id} className={styles.positionListItem}>
                                <PersonPositionCard position={position} />
                            </li>
                        ))}
                    </ul>
                ) : (
                    <div className={styles.noPositions}>No active positions</div>
                )}
            </div>
            <div className={styles.pastPositions}>
                <h4>Past Positions</h4>

                {pastPositions?.length ? (
                    <ul className={styles.positionList}>
                        {pastPositions.map(position => (
                            <li key={position.id} className={styles.positionListItem}>
                                <PersonPositionCard position={position} />
                            </li>
                        ))}
                    </ul>
                ) : (
                    <div className={styles.noPositions}>No past positions</div>
                )}
            </div>
        </div>
    );
};

export default PersonPositionsDetails;
