
import { PositionCard } from '@equinor/fusion-components';
import * as styles from './styles.less';
import { Position } from '@equinor/fusion';
import { isInstanceFuture, isInstancePast } from '../../../../../orgHelpers';
import useInstancesWithPosition, { InstanceWithPosition } from '../hooks/useInstancesWithPosition';
import { FC, useCallback } from 'react';

type DisciplineNetworkTabProps = {
    selectedPosition: Position;
    filterToDate: Date;
    positions: Position[];
};

const DisciplineNetworkTab: FC<DisciplineNetworkTabProps> = ({
    selectedPosition,
    filterToDate,
    positions,
}) => {
    const instances = useInstancesWithPosition(positions);

    const positionDiscipline = selectedPosition.basePosition.discipline;
    const correspondingInstances = positionDiscipline
        ? instances.reduce(
              (uniqueInstances: InstanceWithPosition[], instance: InstanceWithPosition) =>
                  instance.position.basePosition.discipline === positionDiscipline &&
                  !uniqueInstances.some(i => i.position.id === instance.position.id)
                      ? [...uniqueInstances, instance]
                      : uniqueInstances,
              []
          )
        : [];
    const renderDisciplineSection = useCallback(
        (instance: InstanceWithPosition[]) => {
            return (
                <div className={styles.disciplineSection}>
                    {instance.map((instance, index) => (
                        <div className={styles.positionCard} key={index}>
                            <PositionCard
                                showObs={true}
                                instance={instance}
                                position={instance.position}
                                showDate={false}
                                showExternalId
                                showLocation={false}
                                showTimeline={false}
                                isSelected={instance.position.id === selectedPosition.id}
                                selectedDate={filterToDate}
                                isFuture={isInstanceFuture(instance, filterToDate)}
                                isPast={isInstancePast(instance, filterToDate)}
                            />
                        </div>
                    ))}
                </div>
            );
        },
        [selectedPosition, filterToDate]
    );

    return (
        <div className={styles.disciplineNetworkContainer}>
            <div className={styles.disciplineNetwork}>
                <span className={styles.disciplineTitle}>Discipline: {positionDiscipline}</span>
                {renderDisciplineSection(correspondingInstances)}
            </div>
        </div>
    );
};

export default DisciplineNetworkTab;
