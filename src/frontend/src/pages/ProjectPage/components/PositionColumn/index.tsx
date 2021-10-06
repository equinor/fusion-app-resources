
import { useCurrentContext, Position, useApiClients } from '@equinor/fusion';
import { SkeletonBar, PersonPhoto, SkeletonDisc } from '@equinor/fusion-components';
import styles from './styles.less';
import { getInstances } from '../../orgHelpers';
import { FC, useState, useEffect, useMemo } from 'react';

type PositionColumnProps = JSX.IntrinsicElements['div'] & {
    position?: Position | null;
    positionId?: string | null;
};

const PositionColumn: FC<PositionColumnProps> = ({ position, positionId, ...props }) => {
    const [internalPosition, setPosition] = useState<Position | null>(position || null);
    const [isFetching, setIsFetching] = useState(false);
    const currentProject = useCurrentContext();

    const apiClients = useApiClients();
    const fetchPosition = async (projectId: string, id: string) => {
        try {
            setIsFetching(true);
            const response = await apiClients.org.getPositionAsync(projectId, id);
            setPosition(response.data);
        } catch (e) {
            console.error(e);
        }

        setIsFetching(false);
    };

    useEffect(() => {
        if (currentProject?.externalId && positionId) {
            fetchPosition(currentProject.externalId, positionId);
        }
    }, [currentProject, positionId]);

    const instance = useMemo(
        () => internalPosition && getInstances(internalPosition, new Date())[0],
        [internalPosition]
    );

    if (isFetching) {
        return (
            <div  {...props}  className={styles.container}>
                <SkeletonDisc size="medium" />
                <div className={styles.details}>
                    <div className={styles.positionName}>
                        <SkeletonBar />
                    </div>
                    <div className={styles.personName}>
                        <SkeletonBar height={8} />
                    </div>
                </div>
            </div>
        );
    }

    if (!internalPosition) {
        return <>TBN</>;
    }
    return (
        <div  {...props} data-position-id={instance?.id}  className={styles.container}>
            <PersonPhoto person={instance?.assignedPerson || undefined} size="medium" />
            <div className={styles.details}>
                <div className={styles.positionName}>{internalPosition.name}</div>
                <div className={styles.personName}>{instance?.assignedPerson?.name || 'TBN'}</div>
            </div>
        </div>
    );
};

export default PositionColumn;
