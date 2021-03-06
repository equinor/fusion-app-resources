
import styles from './styles.less';
import RemovablePersonDetails from './components/RemovablePersonDetails';
import { PersonPicker } from '@equinor/fusion-components';
import { PersonDetails } from '@equinor/fusion';
import { FC, useCallback } from 'react';

type PeopleSelectorProps = {
    selectedPersons: PersonDetails[];
    setSelectedPersons: (selectedPersons: PersonDetails[]) => void;
};

const PeopleSelector: FC<PeopleSelectorProps> = ({ selectedPersons, setSelectedPersons }) => {
    const removePerson = useCallback(
        (person: PersonDetails) => {
            setSelectedPersons(
                selectedPersons.filter((p) => p.azureUniqueId !== person.azureUniqueId)
            );
        },
        [setSelectedPersons, selectedPersons]
    );

    const addPerson = useCallback(
        (person: PersonDetails) => {
            if (selectedPersons.some((p) => p.azureUniqueId === person.azureUniqueId)) {
                return;
            }
            setSelectedPersons([...selectedPersons, person]);
        },
        [selectedPersons, setSelectedPersons]
    );

    return (
        <div className={styles.container}>
            {selectedPersons.map((person) => (
                <RemovablePersonDetails person={person} onRemove={removePerson} />
            ))}
            <div className={styles.personPicker}>
                <PersonPicker onSelect={addPerson} selectedPerson={null} />
            </div>
        </div>
    );
};

export default PeopleSelector;
