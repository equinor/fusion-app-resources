import * as React from 'react';
import { OrgChartItemProps, PositionCard } from '@equinor/fusion-components';

import * as styles from '../styles.less';
import { isInstancePast, isInstanceFuture } from '../orgHelpers';
import { Position, PositionInstance } from '@equinor/fusion';
import { OrgStructure } from '@equinor/fusion-components';

export type OrgChartCardType = OrgStructure & {
    position: Position;
    instance: PositionInstance | undefined;
};

const OrgChartCard: React.FC<OrgChartItemProps<OrgChartCardType>> = ({ item }) => {
    const filterToDate = React.useMemo(() => new Date(), []);
    const isFuture = React.useMemo(
        () => item.instance && isInstanceFuture(item.instance, filterToDate),
        [item]
    );
    const isPast = React.useMemo(
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
