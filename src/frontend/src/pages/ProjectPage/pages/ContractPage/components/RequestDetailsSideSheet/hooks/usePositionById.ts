import { Position, useApiClients, useCurrentContext } from '@equinor/fusion';
import { useState, useCallback, useEffect } from 'react';
import { useAppContext } from '../../../../../../../appContext';

export default (positionId?: string) => {
    const [position, setPosition] = useState<Position | null>(null);
    const [isFetchingPosition, setIsFetchingPosition] = useState<boolean>(false);
    const [positionError, setPositionError] = useState<Error | null>(null);

    const { dispatchAppAction, appState } = useAppContext();
    const apiClients = useApiClients();

    const currentContext = useCurrentContext();
    const currentOrgProject = currentContext as any;

    const getCachedPosition = useCallback(() => {
        return appState.positions.data.find((pos) => pos.id === positionId);
    }, [positionId, appState]);

    const fetchPositionAsync = useCallback(async (projectId: string, posId: string) => {
        setIsFetchingPosition(true);
        setPositionError(null);
        try {
            const response = await apiClients.org.getPositionAsync(projectId, posId);
            setPosition(response.data);
            dispatchAppAction({
                verb: 'merge',
                collection: 'positions',
                payload: [response.data],
            });
        } catch (e) {
            console.error(e);
            setPositionError(e);
        } finally {
            setIsFetchingPosition(false);
        }
    }, []);

    useEffect(() => {
        const cachedPosition = getCachedPosition();
        if (cachedPosition) {
            setPosition(cachedPosition);
        }
        const projectId = currentOrgProject?.externalId;

        if (projectId && positionId) {
            fetchPositionAsync(projectId as string, positionId);
        }
    }, [positionId, currentOrgProject]);

    return { position, isFetchingPosition, positionError };
};
