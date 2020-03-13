import * as React from 'react';
import { PositionCard } from '@equinor/fusion-components';
import usePositionById from './hooks/usePositionById';

type PositionIdCardProps = {
    positionId?: string;
};
const PositionIdCard: React.FC<PositionIdCardProps> = ({ positionId }) => {
    const { position, isFetchingPosition } = usePositionById(positionId);
    if (!position) {
        return <span>TBN</span>;
    }
    return (
        <PositionCard
            position={position}
            showDate={false}
            showExternalId={false}
            showLocation
            showObs={false}
            showTimeline={false}
            instance={position.instances[0]}
        />
    );
};

export default PositionIdCard;
