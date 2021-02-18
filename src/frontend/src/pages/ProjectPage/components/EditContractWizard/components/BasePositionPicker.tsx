
import {
    SearchableDropdown,
    SearchableDropdownOption,
    SkeletonBar,
    styling,
} from '@equinor/fusion-components';
import { BasePosition } from '@equinor/fusion';
import useBasePositions from '../../../../../hooks/useBasePositions';
import { FC, useMemo, useCallback } from 'react';

type BasePositionPickerProps = {
    selectedBasePositionId?: string;
    onSelect: (basePosition: BasePosition) => void;
};

const BasePositionPicker: FC<BasePositionPickerProps> = ({
    selectedBasePositionId,
    onSelect,
}) => {
    const { basePositions, basePositionsError, isFetchingBasePositions } = useBasePositions();

    const options = useMemo(() => {
        if (basePositionsError || isFetchingBasePositions)
            return []

        return basePositions.map(basePosition => ({
            title: basePosition.name,
            key: basePosition.id,
            isSelected: basePosition.id === selectedBasePositionId,
        }));
    }, [basePositions, basePositionsError, isFetchingBasePositions, selectedBasePositionId]);

    const onDropdownSelect = useCallback(
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
