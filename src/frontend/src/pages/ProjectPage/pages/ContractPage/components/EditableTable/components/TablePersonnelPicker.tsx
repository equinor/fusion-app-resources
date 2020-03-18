import * as React from 'react';
import {
    SearchableDropdownOption,
    SkeletonBar,
    styling,
    SearchableDropdown,
} from '@equinor/fusion-components';
import { DefaultTableType } from './TableTypes';
import Personnel from '../../../../../../../models/Personnel';

function TablePersonnelPicker<T>({
    item,
    onChange,
    accessKey,
    accessor,
    rowIdentifier,
    columnLabel,
    componentState,
}: DefaultTableType<T, Personnel>) {
    const selectedPersonnel = React.useMemo(() => accessor(item), [accessor, item]);

    const options = React.useMemo((): SearchableDropdownOption[] => {
        if (!componentState) {
            return [];
        }
        return componentState.data.map(person => ({
            title: person.name,
            key: person.personnelId,
            isSelected: !!(
                selectedPersonnel && person.personnelId === selectedPersonnel.personnelId
            ),
        }));
    }, [selectedPersonnel, componentState]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            if (!componentState) {
                return;
            }
            const person = componentState.data.find(p => p.personnelId === option.key);
            if (person) {
                onChange(item[rowIdentifier], accessKey, person);
            }
        },
        [onChange, componentState, item, rowIdentifier, accessKey]
    );

    if (componentState?.isFetching) {
        return <SkeletonBar width="100%" height={styling.grid(7)} />;
    }

    return (
        <SearchableDropdown
            label={columnLabel}
            options={options}
            onSelect={onDropdownSelect}
            error={componentState?.error !== null}
            errorMessage="Unable to get personnel"
        />
    );
}

export default TablePersonnelPicker;
