import * as React from 'react';
import {
    SearchableDropdown,
    SearchableDropdownOption,
    SkeletonBar,
    styling,
} from '@equinor/fusion-components';
import { useApiClients, BasePosition, combineUrls, useTelemetryLogger } from '@equinor/fusion';

type BasePositionPickerProps = {
    selectedBasePositionId?: string;
    onSelect: (basePosition: BasePosition) => void;
};

const BasePositionPicker: React.FC<BasePositionPickerProps> = ({
    selectedBasePositionId,
    onSelect,
}) => {
    const apiClients = useApiClients();
    const telemetryLogger = useTelemetryLogger();

    const [basePositions, setBasePositions] = React.useState<BasePosition[]>([]);
    const [isFetchingBasePositions, setIsFetchingBasePositions] = React.useState(false);
    const [basePositionsError, setBasePositionsError] = React.useState<Error | null>(null);
    const fetchBasePositions = async () => {
        setIsFetchingBasePositions(true);
        setBasePositionsError(null);

        try {
            const response = await apiClients.org.getAsync<BasePosition[]>(
                combineUrls('positions', "basepositions?$filter=projectType eq 'PRD-Contracts'")
            );
            setBasePositions(response.data);
        } catch (e) {
            telemetryLogger.trackException(e);
            setBasePositionsError(e);
        }

        setIsFetchingBasePositions(false);
    };

    React.useEffect(() => {
        fetchBasePositions();
    }, []);

    const options = React.useMemo(() => {
        return basePositions.map(basePosition => ({
            title: basePosition.name,
            key: basePosition.id,
            isSelected: basePosition.id === selectedBasePositionId,
        }));
    }, [basePositions, selectedBasePositionId]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            const basePosition = basePositions.find(ba => ba.id === option.key);
            if (basePosition) {
                onSelect(basePosition);
            }
        },
        [onSelect, basePositions]
    );

    if (isFetchingBasePositions) {
        return <SkeletonBar width="100%" height={styling.grid(7)} />;
    }

    return (
        <SearchableDropdown
            label="Base position"
            options={options}
            onSelect={onDropdownSelect}
            error={basePositionsError !== null}
            errorMessage="Unable to get base positions"
        />
    );
};

export default BasePositionPicker;
