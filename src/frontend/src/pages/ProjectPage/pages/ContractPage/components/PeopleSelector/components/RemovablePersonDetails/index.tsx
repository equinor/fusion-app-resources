import styles from "./styles.less";
import { IconButton, DeleteIcon, PersonPhoto } from '@equinor/fusion-components';
import { PersonDetails } from '@equinor/fusion';
import { FC, useCallback } from "react";

type RemovablePersonDetailsProps = {
    person: PersonDetails;
    onRemove: (person: PersonDetails) => void;
};

const RemovablePersonDetails: FC<RemovablePersonDetailsProps> = ({ person, onRemove }) => {
    const removePerson = useCallback(() => onRemove(person), [person]);

    return (
        <div className={styles.personDetailsContainer}>
            <div className={styles.removeContainer}>
                <IconButton onClick={removePerson}>
                    <DeleteIcon outline />
                </IconButton>
            </div>
            <PersonPhoto size="medium" personId={person.azureUniqueId} />
            <div className={styles.details}>
                <span>{person.name}</span>
                {person.mail ? (
                    <a href={`mailto:${person.mail}`}>{person.mail}</a>
                ) : (
                    <span>{person.mail || 'No mail'}</span>
                )}
            </div>
        </div>
    );
};

export default RemovablePersonDetails