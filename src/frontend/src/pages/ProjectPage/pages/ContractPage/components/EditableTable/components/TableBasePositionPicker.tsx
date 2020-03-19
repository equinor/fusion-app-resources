import * as React from 'react';
import { BasePosition } from '@equinor/fusion';
import { DefaultTableType } from './TableTypes';
import {
    SearchableDropdownOption,
    SkeletonBar,
    styling,
    SearchableDropdown,
} from '@equinor/fusion-components';

function TableBasePosition<T, TState extends BasePosition>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    componentState,
}: DefaultTableType<T, TState>) {
    const onBasePositionChange = React.useCallback(
        (basePosition: BasePosition) => {
            onChange(item[rowIdentifier], accessKey, basePosition);
        },
        [onChange, item, accessKey, rowIdentifier]
    );
    const selectedBasePositionId = accessor(item)?.id;

    const options = React.useMemo(() => {
        if (!componentState || componentState.error || componentState.isFetching) {
            return [];
        }
        return componentState.data.map(basePosition => ({
            title: basePosition.name,
            key: basePosition.id,
            isSelected: basePosition.id === selectedBasePositionId,
        }));
    }, [componentState, selectedBasePositionId]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            if (!componentState) {
                return;
            }
            const basePosition = componentState.data.find(ba => ba.id === option.key);
            if (basePosition) {
                onBasePositionChange(basePosition);
            }
        },
        [onBasePositionChange, componentState]
    );

    if (componentState?.isFetching) {
        return <SkeletonBar width="100%" height={styling.grid(7)} />;
    }

    return (
        <SearchableDropdown
            placeholder="Base position"
            options={options}
            onSelect={onDropdownSelect}
            error={componentState?.error !== null}
            errorMessage="Unable to get base positions"
        />
    );
}

export default TableBasePosition;
