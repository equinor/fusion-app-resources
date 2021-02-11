
import { OrgChartItemProps, PositionCard } from '@equinor/fusion-components';

import styles from '../styles.less';
import { isInstancePast, isInstanceFuture } from '../../../../../orgHelpers';
import { Position, PositionInstance } from '@equinor/fusion';
import { OrgStructure } from '@equinor/fusion-components';
import { FC, useMemo } from 'react';

export type OrgChartCardType = OrgStructure & {
    position: Position;
    instance: PositionInstance | undefined;
};

const OrgChartCard: FC<OrgChartItemProps<OrgChartCardType>> = ({ item }) => {
    const filterToDate = useMemo(() => new Date(), []);
    const isFuture = useMemo(
        () => item.instance && isInstanceFuture(item.instance, filterToDate),
        [item]
    );
    const isPast = useMemo(
        () => item.instance && isInstancePast(item.instance, filterToDate),
        [item, filterToDate]
    );

    return (
        <div className={styles.positionCard}>
            <PositionCard
                showObs={true}
                position={item.position}
                instance={item.instance}
                isLinked={item.linked}
                isSelected={true}
                showDate
                showExternalId
                showLocation
                showTimeline
                selectedDate={filterToDate}
                isFuture={isFuture}
                isPast={isPast}
            />
        </div>
    );
};

export default OrgChartCard;
