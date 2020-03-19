import * as React from 'react';
import Personnel, { Position } from '../../../../../../../../models/Personnel';
import * as styles from './styles.less';
import { PersonPosition } from '@equinor/fusion';
import { PersonPositionCard } from '@equinor/fusion-components';

type PositionsTabProps = {
    person: Personnel;
};

const mapPositionsToPersoPosition = (p: Position): PersonPosition => {
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

const PositionsTab: React.FC<PositionsTabProps> = ({ person }) => {
    const positions = person.positions?.map(mapPositionsToPersoPosition);

    let active: PersonPosition[] = [
        {
            id: '37f888d1-dc7b-4eb8-ab12-b5fbb4220b15a',
            name: 'Administrative Lead',

            obs: 'Project Management Team',
            project: {
                id: 'da03f725-29e5-43d9-8f2b-f756873a6034',
                name: 'Åsgard Subsea Compression Phase II with long text',
                domainId: '20396',
                type: '',
            },
            basePosition: {
                id: '3f743b4b-a1e1-414c-8dc9-a3652dab8eb1',
                name: 'Administration Management',

                discipline: 'Quality and Administration',
            },

            appliesFrom: new Date('2019-10-07T00:00:00'),
            appliesTo: new Date('2025-06-30T00:00:00'),
            workload: 25,
        },
        {
            id: '37f888d1-dc7b-4eb8-ab12-b5fbb4220b15b',
            name: 'Administrative Lead',

            obs: 'Project Management Team',
            project: {
                id: 'da03f725-29e5-43d9-8f2b-f756873a6034',
                name: 'Åsgard Subsea Compression Phase II with long text',
                domainId: '20396',
                type: '',
            },
            basePosition: {
                id: '3f743b4b-a1e1-414c-8dc9-a3652dab8eb1',
                name: 'Administration Management',

                discipline: 'Quality and Administration',
            },

            appliesFrom: new Date('2018-01-07T00:00:00'),
            appliesTo: new Date('2019-06-30T00:00:00'),
            workload: 25,
        },
        {
            id: '37f888d1-dc7b-4eb8-ab12-b5fbb4220b15c',
            name: 'Administrative Lead',

            obs: 'Project Management Team',
            project: {
                id: 'da03f725-29e5-43d9-8f2b-f756873a6034',
                name: 'Åsgard Subsea Compression Phase II with long text',
                domainId: '20396',
                type: '',
            },
            basePosition: {
                id: '3f743b4b-a1e1-414c-8dc9-a3652dab8eb1',
                name: 'Administration Management',

                discipline: 'Quality and Administration',
            },

            appliesFrom: new Date('2020-10-07T00:00:00'),
            appliesTo: new Date('2025-06-30T00:00:00'),
            workload: 25,
        },
    ];
    const currentDate = Date.parse(Date());

    return (
        <div className={styles.container}>
            <div className={styles.activePositions}>
                <h4>Active Positions</h4>
                <ul className={styles.positionList}>
                    {active
                        ?.filter(p => p.appliesFrom?.getMilliseconds || currentDate < currentDate)
                        .map(position => (
                            <li key={position.id} className={styles.positionListItem}>
                                <PersonPositionCard position={position} />
                            </li>
                        ))}
                </ul>
            </div>
            <div className={styles.pastPositions}>
                <h4>Past Positions</h4>
                <ul className={styles.positionList}>
                    {active
                        ?.filter(p => p.appliesFrom?.getMilliseconds || currentDate < currentDate)
                        .map(position => (
                            <li key={position.id} className={styles.positionListItem}>
                                <PersonPositionCard position={position} />
                            </li>
                        ))}
                </ul>
            </div>
        </div>
    );
};

export default PositionsTab;
