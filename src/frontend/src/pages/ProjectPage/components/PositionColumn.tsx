import * as React from 'react';
import {
    useCurrentContext,
    Position,
    useApiClients,
} from '@equinor/fusion';
import { PositionCard } from '@equinor/fusion-components';


type PositionColumnProps = { positionId: string | null };
const PositionColumn: React.FC<PositionColumnProps> = ({ positionId }) => {
    const [position, setPosition] = React.useState<Position | null>(null);
    const currentProject = useCurrentContext();

    const apiClients = useApiClients();
    const fetchPosition = async (projectId: string, id: string) => {
        try {
            const response = await apiClients.org.getPositionAsync(projectId, id);
            setPosition(response.data);
        } catch (e) {
            console.error(e);
        }
    };

    React.useEffect(() => {
        if (currentProject && positionId) {
            fetchPosition((currentProject as any).externalId, positionId);
        }
    }, [currentProject, positionId]);

    return position ? (
        <PositionCard
            position={position}
            showDate={false}
            showExternalId={false}
            showLocation={false}
            showObs={false}
            showTimeline={false}
        />
    ) : (
        <>TBN</>
    );
};

export default PositionColumn;