import * as React from 'react';
import * as styles from './styles.less';
import Personnel from '../../../../../../../models/Personnel';

type PersonnelRequestProps = {
    person: Personnel;
};

const PersonnelRequest: React.FC<PersonnelRequestProps> = ({ person }) => {
    return (
        <div className={styles.personnelRequest}>
            <div className={styles.personName}>
                {person.firstName} {person.lastName}
            </div>
            <div className={styles.personMail}>
                <a href={'mailto:' + person.mail}>{person.mail}</a>
            </div>
        </div>
    );
};

export default PersonnelRequest;
