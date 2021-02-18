
import styles from './styles.less';
import { PositionInstance, formatDate, Position } from '@equinor/fusion';
import { Slider, ErrorMessage } from '@equinor/fusion-components';
import { SliderMarker } from '@equinor/fusion-components/dist/components/general/Slider';
import useSortedInstances from '../../hooks/useSortedInstances';
import useArrangedInstance from '../../hooks/useArrangedInstance';
import InstancesTimeline from './InstancesTimeline';
import classNames from 'classnames';
import { getInstances } from '../../../../../../orgHelpers';
import { FC, useMemo, useCallback } from 'react';

export type TimelineStructure = {
    instance: PositionInstance;
    position: Position;
    isSelected?: boolean;
};

type PositionTimelineProps = {
    selectedDate: Date;
    selectedPosition: Position;
};

const createPositionCalculator = (start: number, end: number) => {
    const full = end - start;

    if (full <= 0) {
        throw new Error('No range');
    }

    return (time: number) => Math.min(Math.max(((time - start) / full) * 100, 0), 100);
};

const PositionTimeline: FC<PositionTimelineProps> = ({ selectedDate, selectedPosition }) => {
    const allInstances = selectedPosition.instances;
    const { instancesByFrom } = useSortedInstances(allInstances);
    const { firstInstance, lastInstance } = useArrangedInstance(allInstances);

    const isInstanceSelected = (instance: PositionInstance): boolean => {
        const isSelectedDate =
            instance.appliesFrom.getTime() <= selectedDate.getTime() &&
            instance.appliesTo.getTime() >= selectedDate.getTime();

        if (isSelectedDate) {
            return true;
        }

        return false;
    };

    const timelineStructure: TimelineStructure[] = useMemo(() => {
        return allInstances.map(instance => {
            return {
                instance,
                position: selectedPosition,

                isSelected: isInstanceSelected(instance),
            };
        });
    }, [selectedPosition, allInstances]);

    const currentInstance = useMemo(() => getInstances(selectedPosition, selectedDate)[0], [
        selectedPosition,
        selectedDate,
    ]);

    const calculator = useMemo(() => {
        try {
            return createPositionCalculator(
                firstInstance.appliesFrom.getTime(),
                (lastInstance || firstInstance).appliesTo.getTime()
            );
        } catch {
            return null;
        }
    }, [firstInstance, lastInstance]);

    const labelCheck = useCallback(
        (index: number, instance: PositionInstance): boolean => {
            if (index === 0) {
                return true;
            }

            if (currentInstance && currentInstance.id === instance.id) {
                return true;
            }

            return false;
        },
        [currentInstance]
    );

    const dgDatesSliderMarkers: SliderMarker[] = useMemo(() => {
        const today = new Date().getTime();
        const isTodayOutOfRange =
            firstInstance.appliesFrom.getTime() > today ||
            (lastInstance && lastInstance.appliesTo.getTime() < today);
        const todayMarker: SliderMarker[] = !isTodayOutOfRange
            ? [
                  {
                      value: today,
                      label: 'Today',
                      elevated: true,
                  },
              ]
            : [];
        return [
            {
                value: firstInstance.appliesFrom.getTime(),
                label: '',
            },
            {
                value: lastInstance?.appliesTo.getTime() || 0,
                label: '',
            },
            ...todayMarker,
        ];
    }, [firstInstance, lastInstance]);

    const instancesSliderMarkers: SliderMarker[] = useMemo(() => {
        const markers = instancesByFrom.map((instance, index) => ({
            value: instance.appliesFrom.getTime(),
            label: labelCheck(index, instance) ? formatDate(instance.appliesFrom) : ' ',
        }));
        if (lastInstance) {
            markers.push({
                value: lastInstance.appliesTo.getTime(),
                label: formatDate(lastInstance.appliesTo),
            });
        }
        return markers;
    }, [instancesByFrom, lastInstance]);

    const hasMultipleTimelines = useMemo(() => timelineStructure.length > 1, [
        timelineStructure,
    ]);

    const getLeftPercentage = useCallback((): string => {
        if (!calculator) {
            return '0';
        }
        const range = calculator(selectedDate.getTime());

        if (hasMultipleTimelines) {
            return `calc(${(range / 100) * 90}% + 56px)`;
        }
        return `calc(${range}% - 1px)`;
    }, [hasMultipleTimelines, calculator, selectedDate]);

    const sliderClassNames = classNames(styles.sliderContainer, {
        [styles.hasMultipleTimelines]: hasMultipleTimelines,
    });

    if (!calculator) {
        return (
            <ErrorMessage
                hasError
                title="Invalid date range"
                message="The position start date is equal to or greater than the position end date"
            />
        );
    }

    return (
        <div className={styles.positionTimelineContainer}>
            <div className={styles.selectedDateIndicator} style={{ left: getLeftPercentage() }} />

            <div className={sliderClassNames}>
                <Slider
                    markers={dgDatesSliderMarkers}
                    value={selectedDate.getTime()}
                    disabled={true}
                    onChange={() => {}}
                    key="top-slider"
                />
            </div>
            <div className={styles.timelineInstancesContainer}>
                <InstancesTimeline timeline={timelineStructure} calculator={calculator} />
            </div>

            <div className={sliderClassNames}>
                <Slider
                    markers={instancesSliderMarkers}
                    value={selectedDate.getTime()}
                    onChange={() => {}}
                    disabled={true}
                    hideHandle
                    key="bottom-slider"
                />
            </div>
        </div>
    );
};

export default PositionTimeline;
