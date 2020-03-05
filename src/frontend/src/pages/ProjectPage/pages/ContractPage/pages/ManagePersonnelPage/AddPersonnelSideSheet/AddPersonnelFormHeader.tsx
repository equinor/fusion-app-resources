import * as React from 'react';
import * as classNames from 'classnames'
import * as styles from './styles.less'

type HeaderProps = {
  headers: string[];
}

const AddPersonnelFormHeader: React.FC<HeaderProps> = ({ headers }) => {
  return (
    <>
      <div className={classNames(styles.cell, styles.header)} >
        {headers.map((headerlabel) => (
          <div key={headerlabel} className={styles.label}>
            <span className={styles.label}>{headerlabel}</span>
          </div>
        ))}
      </div>
    </>
  );
}

export default AddPersonnelFormHeader