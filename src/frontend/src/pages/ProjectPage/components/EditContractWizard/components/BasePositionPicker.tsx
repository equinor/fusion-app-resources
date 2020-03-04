import * as React from 'react';
import { SearchableDropdown, SearchableDropdownOption } from '@equinor/fusion-components';
import { useApiClients, BasePosition, combineUrls } from '@equinor/fusion';

type BasePositionPickerProps = {
    selectedBasePositionId?: string;
    onSelect: (basePosition: BasePosition) => void;
};

const BasePositionPicker: React.FC<BasePositionPickerProps> = ({
    selectedBasePositionId,
    onSelect,
}) => {
    const apiClients = useApiClients();

    const [basePositions, setBasePositions] = React.useState<BasePosition[]>([]);
    const fetchBasePositions = async () => {
        const response = await apiClients.org.getAsync<BasePosition[]>(
            combineUrls('positions', "basepositions?$filter=projectType eq 'PRD-Contracts'")
        );
        setBasePositions(response.data);
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

    return (
        <SearchableDropdown label="Base position" options={options} onSelect={onDropdownSelect} />
    );
};

export default BasePositionPicker;
