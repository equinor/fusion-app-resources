import { BareBonePerson } from '..';
import usePersonnel from '../../../pages/ManagePersonnelPage/hooks/usePersonnel';
import {
    SearchableDropdownOption,
    SkeletonBar,
    SearchableDropdown,
} from '@equinor/fusion-components';
import * as React from "react";

type PersonnelPickerProps = {
    onSelect: (person: BareBonePerson) => void;
    selectedPersons: BareBonePerson[];
};

const PersonnelPicker: React.FC<PersonnelPickerProps> = ({ onSelect, selectedPersons }) => {
    const { personnel, isFetchingPersonnel, personnelError } = usePersonnel();

    const options = React.useMemo((): SearchableDropdownOption[] => {
        if (!personnel) {
            return [];
        }
        return personnel
            .filter((person) => person.azureAdStatus === 'Available' && person.azureUniquePersonId)
            .map((person) => ({
                title: person.name,
                key: person.azureUniquePersonId || '',
                isSelected: false,
            }));
    }, [personnel]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            const person = personnel.find((p) => p.azureUniquePersonId === option.key);
            if (
                !person ||
                !person.azureUniquePersonId ||
                selectedPersons.some((p) => p.azureUniqueId === person.azureUniquePersonId)
            ) {
                return;
            }
            onSelect({
                azureUniqueId: person.azureUniquePersonId,
                mail: person.mail,
                name: person.name,
            });
        },
        [personnel, onSelect]
    );

    if (isFetchingPersonnel) {
        return <SkeletonBar />;
    }

    return (
        <SearchableDropdown
            placeholder="Find and add person"
            options={options}
            onSelect={onDropdownSelect}
            error={personnelError !== null}
            errorMessage="Unable to get personnel"
        />
    );
};

export default PersonnelPicker;