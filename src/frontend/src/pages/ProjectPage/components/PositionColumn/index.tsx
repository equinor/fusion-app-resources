import * as React from 'react';
import { useCurrentContext, Position, useApiClients } from '@equinor/fusion';
import { SkeletonBar, PersonPhoto, SkeletonDisc } from '@equinor/fusion-components';
import * as styles from './styles.less';

type PositionColumnProps = {
    position?: Position | null;
    positionId?: string | null
};

const PositionColumn: React.FC<PositionColumnProps> = ({ position, positionId }) => {
    const [internalPosition, setPosition] = React.useState<Position | null>(position || null);
    const [isFetching, setIsFetching] = React.useState(false);
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

    React.useEffect(() => {
        if (currentProject && positionId) {
            fetchPosition((currentProject as any).externalId, positionId);
        }
    }, [currentProject, positionId]);

    const instance = React.useMemo(() => {
        const now = new Date();
        return internalPosition?.instances.find(i => i.appliesFrom <= now && i.appliesTo >= now);
    }, [internalPosition]);

    if (isFetching) {
        return (
            <div className={styles.container}>
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
        <div className={styles.container}>
            <PersonPhoto person={instance?.assignedPerson || undefined} size="medium" />
            <div className={styles.details}>
                <div className={styles.positionName}>{internalPosition.name}</div>
                <div className={styles.personName}>{instance?.assignedPerson?.name || 'TBN'}</div>
            </div>
        </div>
    );
};

export default PositionColumn;
