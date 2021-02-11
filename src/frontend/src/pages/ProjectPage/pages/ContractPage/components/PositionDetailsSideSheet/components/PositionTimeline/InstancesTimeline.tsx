
import * as styles from './styles.less';
import { formatDate } from '@equinor/fusion';
import { useTooltipRef, PersonPhoto, InfoIcon } from '@equinor/fusion-components';
import classNames from 'classnames';
import { TimelineStructure } from '.';
import { FC, useMemo } from 'react';

type InstancesTimelineProps = {
    timeline: TimelineStructure[];
    calculator: (time: number) => number;
};

type TimelineInstanceProps = {
    timelineSplit: TimelineStructure;
    calculator: (time: number) => number;
};

const TimelineInstance: FC<TimelineInstanceProps> = ({ timelineSplit, calculator }) => {
    const instance = timelineSplit.instance;

    const assignedPersonName = instance.assignedPerson ? instance.assignedPerson.name : 'TBN';
    const assignedPersonTooltipRef = useTooltipRef(
        <span>
            {assignedPersonName} <br /> {formatDate(instance.appliesFrom)} -{' '}
            {formatDate(instance.appliesTo)} ({instance.workload}
            %)
        </span>,
        'above'
    );

    const isInstanceSelected = useMemo(() => timelineSplit.isSelected, [timelineSplit]);

    const className = classNames(styles.instance, {
        [styles.hasAssignedPerson]: instance.assignedPerson !== null,
        [styles.isSelected]: isInstanceSelected,
    });

    return (
        <div
            className={className}
            ref={assignedPersonTooltipRef}
            style={{
                left: calculator(instance.appliesFrom.getTime()) + '%',
                right: 100 - calculator(instance.appliesTo.getTime()) + '%',
            }}
        >
            <div className={styles.assignedPerson}>
                <PersonPhoto personId={instance.assignedPerson?.azureUniqueId} size="small" />
            </div>

            <div className={styles.workload}>{instance.workload}%</div>
        </div>
    );
};

const InstancesTimeline: FC<InstancesTimelineProps> = ({ timeline, calculator }) => {
    return (
        <div className={styles.timelineInstancesWithRotation}>
            <div className={styles.timelineInstances}>
                {timeline.map((timelineSplit, index) => (
                    <TimelineInstance
                        calculator={calculator}
                        timelineSplit={timelineSplit}
                        key={timelineSplit.instance.id + index}
                    />
                ))}
            </div>
        </div>
    );
};

export default InstancesTimeline;
