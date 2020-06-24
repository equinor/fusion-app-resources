import { BareBonePerson } from "../..";
import * as React from "react";
import * as styles from "./styles.less";
import { IconButton, DeleteIcon, PersonPhoto } from '@equinor/fusion-components';

type RemovablePersonDetailsProps = {
    person: BareBonePerson;
    onRemove: (person: BareBonePerson) => void;
};

const RemovablePersonDetails: React.FC<RemovablePersonDetailsProps> = ({ person, onRemove }) => {
    const removePerson = React.useCallback(() => onRemove(person), [person]);

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