
import { PositionCard, SkeletonDisc, SkeletonBar } from '@equinor/fusion-components';
import { FC } from 'react';
import usePositionById from './hooks/usePositionById';
import styles from './styles.less';
type PositionIdCardProps = {
    positionId?: string;
};
const PositionIdCard: FC<PositionIdCardProps> = ({ positionId }) => {
    const { position, isFetchingPosition } = usePositionById(positionId);
    if (isFetchingPosition) {
        return (
            <div className={styles.skeletonContainer}>
                <SkeletonDisc size="medium" />
                <div className={styles.details}>
                    <div>
                        <SkeletonBar />
                    </div>
                    <div>
                        <SkeletonBar />
                    </div>
                </div>
            </div>
        );
    }
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
